//MySql imports
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Holostore
{
    class Tools
    {
        public static String concatenateTypes(String originalString, Object value, bool isColumn)
        {
            Type type = value.GetType();


            if (type == typeof(Boolean))
            {
                if (Convert.ToBoolean(value))
                    originalString += "TRUE";
                else
                    originalString += "FALSE";
            }
            else if (type == typeof(Int32))
            {
                originalString += Convert.ToInt32(value);
            }
            else if (type == typeof(String))
            {
                if (!isColumn)
                    originalString += String.Format("\'{0}\'", Convert.ToString(value));
                else
                    originalString += String.Format("{0}", Convert.ToString(value));
            }
            else //In case of a unknown type
            {
                originalString += value;
            }

            return originalString;
        }

        public static Object sqlToC(MySqlDataReader reader, int position)
        {
            String typeName = reader.GetDataTypeName(position);
            if (typeName == "VARCHAR")
                return reader.GetString(position);
            else if (typeName == "INT")
                return reader.GetInt32(position);
            else if (typeName == "DOUBLE")
                return reader.GetDouble(position);
            return null;
        }

    }

    public class Conditions
    {
        public enum Symbols { GREATER_THAN, LESS_THAN, EQUAL, GREATER_EQUAL_THAN, LESS_EQUAL_THAN, NO_EQUAL }; 

        private String field;
        private Object value;
        private Symbols operation;

        public Conditions(String field, Object value, Symbols operation)
        {
            this.field = field;
            this.value = value;
            this.operation = operation;
        }

        private String getOperationSymbol()
        {
            switch (this.operation)
            {
                case Symbols.GREATER_THAN:
                    return ">";
                case Symbols.LESS_THAN:
                    return "<";
                case Symbols.EQUAL:
                    return "=";
                case Symbols.GREATER_EQUAL_THAN:
                    return ">=";
                case Symbols.LESS_EQUAL_THAN:
                    return "<=";
                case Symbols.NO_EQUAL:
                    return "<>";
                default:
                    return "";
            }
        }

        public String getExpression()
        {
            
            String finalExpression = String.Format("{0} {1} ",this.field,getOperationSymbol());
            finalExpression = Tools.concatenateTypes(finalExpression, this.value, false);

            return finalExpression;
        }

        #region Properties
        public void setField(String value)
        {
            this.field = value;
        }

        public String getField()
        {
            return this.field;
        }

        public void setValue(Object value)
        {
            this.value = value;
        }

        public Object getValue()
        {
            return this.value;
        }

        public void setOperation(Symbols operation)
        {
            this.operation = operation;
        }

        public Symbols getOperation()
        {
            return this.operation;
        } 
        #endregion

    }

    public class DBConnector
    {
        public const long INSERTION_ERROR = -1;
        
        private String connectionString;
        private MySqlConnection connection;
        public DBConnector(String dbAddress, String databaseName, String username, String password)
        {
            //generating the connection string
            this.connectionString = String.Format("server={0};database={1};uid={2};pwd={3};",
                                    dbAddress,
                                    databaseName,
                                    username,
                                    password);
            this.connection = new MySqlConnection(connectionString);
        }


        public bool connect()
        {
            if (this.connectionString.Equals(null))
            {
                return false;
            }
            else
            {
                try
                {
                    connection.Open();
                    return true;
                } catch (Exception ex)
                {
                    return false;
                }

            }
        }

        public void close()
        {
            connection.Close();
        }


        public long insertQuery(String table, Object[] columns, Object[] values)
        {
            //Missmatch select columns with values
            if (columns.Length != values.Length)
            {
                throw new ArgumentException("The number of values must match with the number of columns");
            }

            try
            {
                String query = String.Format("INSERT INTO {0} ", table);

                String valuesString = "";
                String columnsString = "";

                for (int i = 0; i < columns.Length; i++)
                {
                    if (i > 0)
                    {
                        valuesString += ", ";
                        columnsString += ", ";
                    }
                    valuesString = Tools.concatenateTypes(valuesString, values[i], false);
                    columnsString = Tools.concatenateTypes(columnsString, columns[i], true);
                }

                valuesString = "(" + valuesString + ")";
                columnsString = "(" + columnsString + ")";

                query += String.Format("{0} VALUES {1};", columnsString, valuesString);

                Console.WriteLine(query);
                MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.ExecuteNonQuery();
                Console.WriteLine(cmd.LastInsertedId);
                return cmd.LastInsertedId;
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString()); 
                return INSERTION_ERROR;
            }

        }

        public LinkedList<Dictionary<String, Object>> selectQuery(String[] tables, String[] fields, Conditions[] conditions)
        {
            String fieldsString = "";
            String tablesString = "";
            String conditionString = "";
            String query = "SELECT ";

            if (tables.Length == 0) //some table should be specified
                throw new ArgumentException("The number of tables must be at least 1");

            for (int i = 0; i < fields.Length; i++)
            {
                if (i > 0)
                    fieldsString += ", ";

                fieldsString = Tools.concatenateTypes(fieldsString, fields[i], true);
            }

            for (int i = 0; i < tables.Length; i++)
            {
                if (i > 0)
                    tablesString += ", ";
                tablesString = Tools.concatenateTypes(tablesString, tables[i], true);
            }

            //if no fields are specified is because we want all the columns
            if (fields.Length == 0)
            {
                query += "* ";
            }
            else
            {
                query += String.Format("{0} ", fieldsString);
            }

            query += String.Format("FROM {0}", tablesString);
            if ((conditions != null) && (conditions.Length > 0) )
            {
                for (int i = 0; i < conditions.Length; i++)
                {
                    if (i > 0)
                        conditionString += " AND ";
                    conditionString += String.Format(conditions[i].getExpression());
                }

            
                query += String.Format(" WHERE {0};", conditionString);
            }

            Console.WriteLine(query);
            var cmd = new MySqlCommand(query, connection);
            var reader = cmd.ExecuteReader();

            LinkedList<Dictionary<String, Object>> search = new LinkedList<Dictionary<String, Object>>();
            
            int j = 0;
            while (reader.Read())
            {
                search.AddFirst(new Dictionary<string, object>());
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    search.First.Value.Add(fields[i],Tools.sqlToC(reader,i));
                }
                j++;
            }
            reader.Close();
            return search;
        }

        /******************************DB_FIELDS*******************************/
        /**********************************************************************/
        const String FURNITURE_TABLE = "furnitureGeneralInfo";

        const String furnitureId = "furnitureId";
        const String furnitureName = "furnitureName";
        const String description = "description";
        const String dimensions = "dimensions";
        const String furnitureTypeId = "furnitureTypeId";
        const String colorId = "colorId";
        const String manufacturerId = "manufacturerId";
        const String price = "price";

        /*********************************DB Color***********************************/
        /****************************************************************************/

        const String FURNITURE_COLOR_TABLE = "furnitureColor";

        const String colorName = "colorName";

        /******************************DB Manufacturer*******************************/
        /****************************************************************************/

        const String MANUFACTURER_TABLE = "manufacturers";

        const String manufacturerName = "manufacturerName";

        /******************************DB Furniture Type*****************************/
        /****************************************************************************/
        const String FURNITURE_TYPE_TABLE = "furnitureType";

        const String typeName = "typeName";

        /******************************DB Cloud Path*****************************/
        /****************************************************************************/
        const String CLOUD_PATH_TABLE = "cloudPath";

        const String objectType = "objectType";
        const String path = "path";

        /*********************************DB CardType***********************************/
        /****************************************************************************/

        const String CARD_TYPE_TABLE = "cardType";

        const String cardTypeId = "cardTypeId";
        const String cardName = "cardName";

        /*********************************DB Order***********************************/
        /****************************************************************************/
        const String ORDER_TABLE = "orderHistory";

        const String orderId = "orderId";
        const String userId = "userId";
        const String orderDate = "orderDate";

        /*********************************DB User***********************************/
        /****************************************************************************/
        const String USER_TABLE = "userGneralInfo";

        const String firstName = "firstName";
        const String lastName = "lastName";
        const String address = "address";
        const String city = "city";
        const String state = "state";
        const String zip = "zip";
        const String phone = "phone";
        const String sex = "sex";

        /*********************************DB UserCredentials***********************************/
        /****************************************************************************/
        const String USER_CREDENTIALS_TABLE = "userLoginCredentials";

        const String username = "username";
        const String password = "password";
        const String userLevel = "userLevel";

        /*********************************DB Inventory***********************************/
        /****************************************************************************/
        const String INVENTORY_TABLE = "inventory";

        const String quantity = "quantity";

        public Manufacturer getManufacturer(int id)
        {
            String[] tables = new String[1] { MANUFACTURER_TABLE };
            String[] fields = new String[2] {manufacturerId,
                                            manufacturerName};
            Conditions[] conditions = new Conditions[1] { new Conditions(manufacturerId, id, Conditions.Symbols.EQUAL) };
            LinkedList<Dictionary<String, Object>> result = selectQuery(tables, fields, conditions);
            Manufacturer manufacturer;
            if (result.Count > 0)
            {
                manufacturer = new Manufacturer((int)result.First.Value[manufacturerId],
                                                (String)result.First.Value[manufacturerName]);
                return manufacturer;
            }
            else
            {
                return null;
            }

        }
        public Color_ getColor(int id)
        {
            String[] tables = new String[1] { FURNITURE_COLOR_TABLE };
            String[] fields = new String[2] {colorId,
                                            colorName};
            Conditions[] conditions = new Conditions[1] { new Conditions(colorId, id, Conditions.Symbols.EQUAL) };
            LinkedList<Dictionary<String, Object>> result = selectQuery(tables, fields, conditions);
            Color_ color;
            if (result.Count > 0)
            {
                color = new Color_((int)result.First.Value[colorId],
                                                (String)result.First.Value[colorName]);
                return color;
            }
            else
            {
                return null;
            }


        }
        public FurnitureType getFurnitureType(int id)
        {
            String[] tables = new String[1] { FURNITURE_TYPE_TABLE };
            String[] fields = new String[2] {furnitureTypeId,
                                            typeName};
            Conditions[] conditions = new Conditions[1] { new Conditions(furnitureTypeId, id, Conditions.Symbols.EQUAL) };
            LinkedList<Dictionary<String, Object>> result = selectQuery(tables, fields, conditions);
            FurnitureType furnitureType;
            if (result.Count > 0)
            {
                furnitureType = new FurnitureType((int)result.First.Value[furnitureTypeId],
                                                (String)result.First.Value[typeName]);
                return furnitureType;
            }
            else
            {
                return null;
            }


        }
        public CloudPath[] getCloudPath(int furniture_id)
        {
            String[] tables = new String[1] { CLOUD_PATH_TABLE };
            String[] fields = new String[3] {objectType,
                                            furnitureId,
                                            path};
            Conditions[] conditions = new Conditions[1] { new Conditions(furnitureId, furniture_id, Conditions.Symbols.EQUAL) };
            LinkedList<Dictionary<String, Object>> result = selectQuery(tables, fields, conditions);

            if (result.Count > 0)
            {
                int itemCount = result.Count;
                CloudPath[] cloudPaths = new CloudPath[itemCount];
                int i = 0;
                foreach (var item in result)
                {
                    cloudPaths[i] = new CloudPath((int)item[furnitureId],
                                                  (String)item[path],
                                                  (String)item[objectType]);

                    i++;
                }

                return cloudPaths;

            }
            else
                return null;
        }
        public Furniture getFurniture(int id)
        {
            String[] tables = new String[1] { FURNITURE_TABLE };
            String[] fields = new String[8] {furnitureId,
                                            furnitureName,
                                            description,
                                            dimensions,
                                            furnitureTypeId,
                                            colorId,
                                            manufacturerId,
                                            price};
            Conditions[] conditions = new Conditions[1] { new Conditions(furnitureId, id, Conditions.Symbols.EQUAL) };
            LinkedList<Dictionary<String, Object>> result = selectQuery(tables, fields, conditions);

            Furniture furniture;

            if (result.Count > 0)
            {
                Color_ color = getColor((int)result.First.Value[colorId]);
                Manufacturer manufacturer = getManufacturer((int)result.First.Value[manufacturerId]);
                FurnitureType type = getFurnitureType((int)result.First.Value[furnitureTypeId]);

                if (color == null)
                {
                    throw new NullReferenceException("There is no color with id number " + (int)result.First.Value[colorId]);
                }
                if (manufacturer == null)
                {
                    throw new NullReferenceException("There is no manufacturer with id number " + (int)result.First.Value[manufacturerId]);
                }
                if (type == null)
                {
                    throw new NullReferenceException("There is no type with id number " + (int)result.First.Value[furnitureTypeId]);
                }

                furniture = new Furniture((int)result.First.Value[furnitureId],
                                            (String)result.First.Value[furnitureName],
                                            (String)result.First.Value[description],
                                            (String)result.First.Value[dimensions],
                                            type,
                                            color,
                                            manufacturer,
                                            (double)result.First.Value[price]
                                            );
                return furniture;
            }
            else
            {
                return null;
            }


        }
        public CardType getCardType(int id)
        {
            String[] tables = new String[1] { CARD_TYPE_TABLE };
            String[] fields = new String[2] {cardTypeId,
                                            cardName};
            Conditions[] conditions = new Conditions[1] { new Conditions(cardTypeId, id, Conditions.Symbols.EQUAL) };
            LinkedList<Dictionary<String, Object>> result = selectQuery(tables, fields, conditions);
            CardType card;
            if (result.Count > 0)
            {
                card = new CardType((int)result.First.Value[cardTypeId],
                                                (String)result.First.Value[cardName]);
                return card;
            }
            else
            {
                return null;
            }
        }
        public Order getOrder(int id)
        {
            String[] tables = new String[1] { ORDER_TABLE };
            String[] fields = new String[4] {orderId,
                                            userId,
                                            furnitureId,
                                            orderDate};
            Conditions[] conditions = new Conditions[1] { new Conditions(orderId, id, Conditions.Symbols.EQUAL) };
            LinkedList<Dictionary<String, Object>> result = selectQuery(tables, fields, conditions);
            Order order;
            if (result.Count > 0)
            {
                order = new Order((int)result.First.Value[orderId],
                                  (int)result.First.Value[userId],
                                  (int)result.First.Value[furnitureId],
                                  (DateTime)result.First.Value[orderDate]);
                return order;
            }
            else
            {
                return null;
            }
        }
        public User getUser(int id)
        {
            String[] tables = new String[1] { USER_TABLE };
            String[] fields = new String[9] {userId,
                                            firstName,
                                            lastName,
                                            address,
                                            city,
                                            state,
                                            zip,
                                            phone,
                                            sex};
            Conditions[] conditions = new Conditions[1] { new Conditions(userId, id, Conditions.Symbols.EQUAL) };
            LinkedList<Dictionary<String, Object>> result = selectQuery(tables, fields, conditions);
            User user;
            if (result.Count > 0)
            {
                user = new User((int)result.First.Value[userId],
                                  (String)result.First.Value[firstName],
                                  (String)result.First.Value[lastName],
                                  (String)result.First.Value[address],
                                  (String)result.First.Value[city],
                                  (String)result.First.Value[state],
                                  (String)result.First.Value[zip],
                                  (String)result.First.Value[phone],
                                  (String)result.First.Value[sex]
                                  );
                return user;
            }
            else
            {
                return null;
            }
        }
        public UserCredentials getUserCredentials(int id)
        {
            String[] tables = new String[1] { USER_CREDENTIALS_TABLE };
            String[] fields = new String[4] {username,
                                            password,
                                            userId,
                                            userLevel
                                            };
            Conditions[] conditions = new Conditions[1] { new Conditions(userId, id, Conditions.Symbols.EQUAL) };
            LinkedList<Dictionary<String, Object>> result = selectQuery(tables, fields, conditions);
            UserCredentials credentials;
            if (result.Count > 0)
            {
                credentials = new UserCredentials((int)result.First.Value[userId],
                                  (String)result.First.Value[username],
                                  (String)result.First.Value[password],
                                  (UserCredentials.UserLevel)result.First.Value[userLevel]
                                  );
                return credentials;
            }
            else
            {
                return null;
            }
        }
        public InventoryItem getInventoryItem(int id)
        {

            String[] tables = new String[1] { INVENTORY_TABLE };
            String[] fields = new String[2] {furnitureId,
                                            quantity};
            Conditions[] conditions = new Conditions[1] { new Conditions(furnitureId, id, Conditions.Symbols.EQUAL) };
            LinkedList<Dictionary<String, Object>> result = selectQuery(tables, fields, conditions);
            InventoryItem item;
            if (result.Count > 0)
            {
                item = new InventoryItem((int)result.First.Value[furnitureId],
                                                (int)result.First.Value[quantity]);
                return item;
            }
            else
            {
                return null;
            }
        }

        public User[] getAllUsers()
        {
            String[] tables = new String[1] { USER_TABLE };
            String[] fields = new String[9] {userId,
                                            firstName,
                                            lastName,
                                            address,
                                            city,
                                            state,
                                            zip,
                                            phone,
                                            sex};

            LinkedList<Dictionary<String, Object>> result = selectQuery(tables, fields, null);
            if (result.Count > 0)
            {
                int itemCount = result.Count;
                User[] users = new User[itemCount];
                int i = 0;
                foreach (var item in result)
                {
                    users[i] = new User((int)item[userId],
                                             (String)item[firstName],
                                             (String)item[lastName],
                                             (String)item[address],
                                             (String)item[city],
                                             (String)item[state],
                                             (String)item[zip],
                                             (String)item[phone],
                                             (String)item[sex]
                                             );
                    i++;
                }





                return users;
            }
            else
            {
                return null;
            }

        }
        public Furniture[] getAllFurniture()
        {
            String[] tables = new String[1] { FURNITURE_TABLE };
            String[] fields = new String[8] {furnitureId,
                                            furnitureName,
                                            description,
                                            dimensions,
                                            furnitureTypeId,
                                            colorId,
                                            manufacturerId,
                                            price};

            LinkedList<Dictionary<String, Object>> result = selectQuery(tables, fields, null);
            if (result.Count > 0)
            {
                int itemCount = result.Count;
                Furniture[] furnitures = new Furniture[itemCount];
                int i = 0;
                foreach (var item in result)
                {
                    Color_ color = getColor((int)item[colorId]);
                    Manufacturer manufacturer = getManufacturer((int)item[manufacturerId]);
                    FurnitureType type = getFurnitureType((int)item[furnitureTypeId]);
                    if (color == null)
                    {
                        throw new NullReferenceException("There is no color with id number " + (int)result.First.Value[colorId]);
                    }
                    if (manufacturer == null)
                    {
                        throw new NullReferenceException("There is no manufacturer with id number " + (int)result.First.Value[manufacturerId]);
                    }
                    if (type == null)
                    {
                        throw new NullReferenceException("There is no type with id number " + (int)result.First.Value[furnitureTypeId]);
                    }

                    furnitures[i] = new Furniture((int)item[furnitureId],
                                            (String)item[furnitureName],
                                            (String)item[description],
                                            (String)item[dimensions],
                                            type,
                                            color,
                                            manufacturer,
                                            (double)item[price]
                                            );
                    i++;
                }

                return furnitures;
            }
            else
            {
                return null;
            }
        }
        public Color_[] getAllColors()
        {
            String[] tables = new String[1] { FURNITURE_COLOR_TABLE };
            String[] fields = new String[2] {colorId,
                                            colorName};

            LinkedList<Dictionary<String, Object>> result = selectQuery(tables, fields, null);

            if (result.Count > 0)
            {
                int itemCount = result.Count;
                Color_[] colors = new Color_[itemCount];
                int i = 0;
                foreach (var item in result)
                {


                    colors[i] = new Color_((int)item[colorId],
                                            (String)item[colorName]
                                            );
                    i++;
                }

                return colors;
            }
            else
            {
                return null;
            }
        }
        public Manufacturer[] getAllManufactuers()
        {
            String[] tables = new String[1] { MANUFACTURER_TABLE };
            String[] fields = new String[2] {manufacturerId,
                                            manufacturerName};

            LinkedList<Dictionary<String, Object>> result = selectQuery(tables, fields, null);
            if (result.Count > 0)
            {
                int itemCount = result.Count;
                Manufacturer[] manufacturers = new Manufacturer[itemCount];
                int i = 0;
                foreach (var item in result)
                {


                    manufacturers[i] = new Manufacturer((int)item[manufacturerId],
                                            (String)item[manufacturerName]
                                            );
                    i++;
                }

                return manufacturers;
            }
            else
            {
                return null;
            }
        }
        public Order[] getAllOrders()
        {
            String[] tables = new String[1] { ORDER_TABLE };
            String[] fields = new String[4] {orderId,
                                            userId,
                                            furnitureId,
                                            orderDate};

            LinkedList<Dictionary<String, Object>> result = selectQuery(tables, fields, null);

            if (result.Count > 0)
            {
                int itemCount = result.Count;
                Order[] orders = new Order[itemCount];
                int i = 0;
                foreach (var item in result)
                {


                    orders[i] = new Order((int)item[orderId],
                                          (int)item[userId],
                                          (int)item[furnitureId],
                                          (DateTime)item[orderDate]
                                            );
                    i++;
                }

                return orders;
            }
            else
            {
                return null;
            }
        }
        public FurnitureType[] getAlFurnitureTypes()
        {
            String[] tables = new String[1] { FURNITURE_TYPE_TABLE };
            String[] fields = new String[2] {furnitureTypeId,
                                            typeName};

            LinkedList<Dictionary<String, Object>> result = selectQuery(tables, fields, null);

            if (result.Count > 0)
            {
                int itemCount = result.Count;
                FurnitureType[] furnitureTypes = new FurnitureType[itemCount];
                int i = 0;
                foreach (var item in result)
                {


                    furnitureTypes[i] = new FurnitureType((int)item[furnitureTypeId],
                                            (String)item[typeName]
                                            );
                    i++;
                }

                return furnitureTypes;
            }
            else
            {
                return null;
            }
        }

        public bool insertFurniture(Furniture newFurniture, CloudPath cloudPath)
        {
            FurnitureType type = newFurniture.getFurnitureType();
            Color_ color = newFurniture.getColor();
            Manufacturer manufacturer = newFurniture.getManufacturer();
            Object[] columns = new Object[7] {furnitureName,
                                                description,
                                                dimensions,
                                                furnitureTypeId,
                                                colorId,
                                                manufacturerId,
                                                price
                                                };

            Object[] values = new Object[7] {newFurniture.getName(),
                                                newFurniture.getDescription(),
                                                newFurniture.getDimensions(),
                                                type.getId(),
                                                color.getId(),
                                                manufacturer.getId(),
                                                newFurniture.getPrice()
                                                };

            long furnitureResult = this.insertQuery(FURNITURE_TABLE, columns, values);

            if (furnitureResult != DBConnector.INSERTION_ERROR)
                cloudPath.setFurnitureId((int)furnitureResult);
            else
                return false;

            bool cloudPathResult = inserCloudPath(cloudPath);

            return cloudPathResult;
        }
        public bool insertManufactuer(Manufacturer newManufacturer)
        {
            Object[] columns = new Object[1] {manufacturerName
                                                };

            Object[] values = new Object[1] {newManufacturer.getName()
                                                };

            return (this.insertQuery(MANUFACTURER_TABLE, columns, values) != DBConnector.INSERTION_ERROR);
        }
        public bool insertColor(Color_ newColor)
        {
            Object[] columns = new Object[1] {colorName
                                                };

            Object[] values = new Object[1] {newColor.getName()
                                                };

            return this.insertQuery(FURNITURE_COLOR_TABLE, columns, values) != DBConnector.INSERTION_ERROR;
        }
        public bool insertFurnitureType(FurnitureType newFurnitureType)
        {
            Object[] columns = new Object[1] {typeName
                                                };

            Object[] values = new Object[1] {newFurnitureType.getName()
                                                };

            return this.insertQuery(FURNITURE_TYPE_TABLE, columns, values) != DBConnector.INSERTION_ERROR;
        }
        public bool inserCloudPath(CloudPath newCloudPath)
        {
            Object[] columns = new Object[3] {objectType,
                                              furnitureId,
                                              path
                                                };

            Object[] values = new Object[3] {newCloudPath.getObjectType(),
                                                newCloudPath.getFurnitureId(),
                                                newCloudPath.getPath()
                                                };

            return this.insertQuery(CLOUD_PATH_TABLE, columns, values) != DBConnector.INSERTION_ERROR;
        }

        public UserCredentials.UserLevel checkCredentials(String user_name, String pwd)
        {
            String[] tables = new String[1] { USER_CREDENTIALS_TABLE };
            String[] fields = new String[4] {username,
                                            password,
                                            userId,
                                            userLevel
                                            };
            Conditions[] conditions = new Conditions[2] { new Conditions(username, user_name, Conditions.Symbols.EQUAL),
                                                          new Conditions(password, pwd, Conditions.Symbols.EQUAL)};
            LinkedList<Dictionary<String, Object>> result = selectQuery(tables, fields, conditions);
            UserCredentials credentials;
            if (result.Count > 0)
            {
                credentials = new UserCredentials((int)result.First.Value[userId],
                                  (String)result.First.Value[username],
                                  (String)result.First.Value[password],
                                  (UserCredentials.UserLevel)result.First.Value[userLevel]
                                  );

                return credentials.getUserLevel();
            }
            else
            {
                return UserCredentials.UserLevel.USER_LEVEL_ERROR;
            }
        }

    }

    /***************************************************************************
     * Main objects of the database
     ***************************************************************************/
    public class Manufacturer
    {
        private int id; //manufacturerId field
        private String name; //manufacturerName field

        public Manufacturer(String name)
        {
            this.id = 0;
            this.name = name;
        }
        public Manufacturer(int id, String name)
        {
            this.id = id;
            this.name = name;
        }

        #region Properties
        public int getId()
        {
            return this.id;
        }

        public void setId(int value)
        {
            this.id = value;
        }

        public String getName()
        {
            return this.name;
        }

        public void setName(String value)
        {
            this.name = value;
        }

        #endregion
    }

    public class Color_
    {
        private int id; //colorId field
        private String name; //colorName field
        public Color_(String name)
        {
            this.id = 0;
            this.name = name;
        }
        public Color_(int id, String name)
        {
            this.id = id;
            this.name = name;
        }
        #region Properties
        public int getId()
        {
            return this.id;
        }

        public void setId(int value)
        {
            this.id = value;
        }

        public String getName()
        {
            return this.name;
        }

        public void setName(String value)
        {
            this.name = value;
        }
        #endregion
    }
    public class FurnitureType
    {
        private int id; //furnitureTypeId field
        private String name;    //typeName

        public FurnitureType(String name)
        {
            this.id = 0;
            this.name = name;
        }

        public FurnitureType(int id, String name)
        {
            this.id = id;
            this.name = name;
        }

        #region Properties
        public String getName()
        {
            return this.name;
        }

        public void setName(String value)
        {
            this.name = value;
        }

        public int getId()
        {
            return this.id;
        }

        public void setId(int value)
        {
            this.id = value; 
        }

        #endregion

    }
    public class Furniture
    {
        private int id; //furnitureId field
        private String name;    //furnitureName field
        private String description; //description field
        private String dimensions; //dimensions field
        private FurnitureType type; //furnitureTypeId field
        private Color_ color; //colorId field
        private Manufacturer manufacturer; //manufacturerId field
        private double price; //price field

        public Furniture(int id, 
                        String name,
                        String description,
                        String dimensions,
                        FurnitureType type,
                        Color_ color,
                        Manufacturer manufacturer,
                        double price) {
            this.id = id;
            this.name = name;
            this.description = description;
            this.dimensions = dimensions;
            this.type = type;
            this.color = color;
            this.manufacturer = manufacturer;
            this.price = price;
        }
        public Furniture(String name,
                       String description,
                       String dimensions,
                       FurnitureType type,
                       Color_ color,
                       Manufacturer manufacturer,
                       double price)
        {
            this.id = 0;
            this.name = name;
            this.description = description;
            this.dimensions = dimensions;
            this.type = type;
            this.color = color;
            this.manufacturer = manufacturer;
            this.price = price;
        }

        #region Properties
        public int getId()
        {
            return this.id;
        }
        public void setId(int value)
        {
            this.id = value;
        }
        public String getName()
        {
            return this.name;
        }
        public void setName(String value)
        { 
            this.name = value;
        }
        public String getDescription()
        {
            return this.description;
        }
        public void setDescription(String value)
        {
            this.description = value;
        }
        public String getDimensions()
        {
            return this.dimensions;
        }
        public void setDimensions(String value)
        {
            this.dimensions = value;
        }
        public FurnitureType getFurnitureType()
        {
            return this.type;
        }
        public void setFurnitureType(FurnitureType value)
        {
            this.type = value;
        }
        public Color_ getColor()
        {
            return this.color;
        }
        public void setColor(Color_ value) {
            this.color = value;
        }
        public Manufacturer getManufacturer()
        {
            return this.manufacturer;
        }
        public void setManufacturer(Manufacturer value)
        {
            this.manufacturer = value;
        }
        public double getPrice()
        {
            return this.price;
        }
        public void setPrice(double value)
        {
            this.price = value;
        }

        #endregion
    }

    public class UserCredentials
    {
        public enum UserLevel { GUEST, REGISTERED, ADMIN, USER_LEVEL_ERROR };

        private int id; //userId field
        private String username;  //username field
        private String password;  //password field
        private UserLevel userLevel; //userLevel field

        public UserCredentials(int id, String username, String password, UserLevel userLevel)
        {
            this.id = id;
            this.userLevel = userLevel;
            this.username = username;
            this.password = password;
        }

        #region Properties
        public int getId()
        {
            return this.id;
        }
        public void setId(int value)
        {
            this.id = value;
        }
        public String getUsername()
        {
            return this.username;
        }
        public void setUsername(String value)
        {
            this.username = value;
        }
        public String getPassword()
        {
            return this.password;
        }
        public void setPassword(String value)
        {
            this.password = value;
        }
        public UserLevel getUserLevel()
        {
            return this.userLevel;
        }
        public void setUserLevel(UserLevel value)
        {
            this.userLevel = value;
        }
        #endregion
    }
    public class User
    {
        private int id;
        private String firstName;
        private String lastName;
        private String address;
        private String city;
        private String state;
        private String zip;
        private String phone;
        private String sex;

        public User(int id,
                    String firstName,
                    String lastName,
                    String address,
                    String city,
                    String state,
                    String zip,
                    String phone,
                    String sex)
        {
            this.id = id;
            this.lastName = lastName;
            this.firstName = firstName;
            this.address = address;
            this.city = city;
            this.state = state;
            this.zip = zip;
            this.phone = phone;
            this.sex = sex;
        }

        #region Properties
        public int getId()
        {
            return this.id;
        }
        public void setId(int value)
        {
            this.id = value;
        }
        public String getFirstName()
        {
            return this.firstName;
        }
        public void setFirstName(String value)
        {
            this.firstName = value;
        }
        public String getLastName()
        {
            return this.lastName;
        }
        public void setLastName(String value)
        {
            this.lastName = value;
        }
        public String getAddress()
        {
            return this.address;
        }
        public void setAddress(String value)
        {
            this.address = value;
        }
        public String getCity()
        {
            return this.city;
        }
        public void setCity(String value)
        {
            this.city = value;
        }
        public String getState()
        {
            return this.state;
        }
        public void setState(String value)
        {
            this.state = value;
        }
        public String getZip()
        {
            return this.zip;
        }
        public void setZip(String value)
        {
            this.zip = value;
        }
        public String getPhone()
        {
            return this.phone;
        }
        public void setPhone(String value)
        {
            this.phone = value;
        }
        public String getSex()
        {
            return this.sex;
        }
        public void setSex(String value)
        {
            this.sex = value;
        }
        #endregion

    }

    public class CardType {
        private int id;  //cardTypeId field
        private String name; //cardName field

        public CardType(int id, String name)
        {
            this.id = id;
            this.name = name;
        }

        #region Properties
        public int getId()
        {
            return this.id;
        }
        public void setId(int value)
        {
            this.id = value;
        }
        public String getName()
        {
            return this.name;
        }
        public void setName(String value)
        {
            this.name = value;
        }
        #endregion
    }
    public class Payment
    {
        private int id; //paymentId field
        private int userId;  //userID field
        private CardType cardType; //cardTypeId field
        private String cardNumber; //cardNumber field
        private DateTime date; //date field

        public Payment(int id, int userId, CardType cardType,String cardNumber, DateTime date)
        {
            this.id = id;
            this.userId = userId;
            this.cardType = cardType;
            this.cardNumber = cardNumber;
            this.date = date;
        }

        #region Properties
        public int getId()
        {
            return this.id;
        }
        public void setId(int value)
        {
            this.id = value;
        }
        public int getUserId()
        {
            return this.userId;
        }
        public void setUserId(int value)
        {
            this.userId = value;
        }
        public CardType getCardType()
        {
            return this.cardType;
        }
        public void setCardTypeId(CardType value)
        {
            this.cardType = value;
        }
        public String getCardNumber()
        {
            return this.cardNumber;
        }
        public void setCardNumber(String value)
        {
            this.cardNumber = value;
        }
        public DateTime getDate()
        {
            return this.date;
        }
        public void setDate(DateTime value)
        {
            this.date = value;
        }
        #endregion
    }

    public class CloudPath
    {
        //the only 2 types of models that Amazon is gonna storage
        public const String D_2 = "2D";
        public const String D_3 = "3D";

        private int furnitureId; //furnitureId field
        private String path;  //path field
        private String objectType; //objectType field

        public CloudPath(int furnitureId, String path, String objectType)
        {
            this.furnitureId = furnitureId;
            this.path = path;
            this.objectType = objectType;
        }

        public int getFurnitureId()
        {
            return this.furnitureId;
        }
        public void setFurnitureId(int value)
        {
            this.furnitureId = value;
        }
        public String getPath()
        {
            return this.path;
        }
        public void setPath(String value)
        {
            this.path = value;
        }
        public String getObjectType()
        {
            return this.objectType;
        }
        public void setObjectType(String value)
        {
            this.objectType = value;
        }
    }
    public class Order
    {
        private int id; //orderId field
        private int userId; //userId field
        private int furnitureId; //furnitureId field
        private DateTime date; //date field

        public Order(int id, int userId, int furnitureId, DateTime date)
        {
            this.id = id;
            this.userId = userId;
            this.furnitureId = furnitureId;
            this.date = date;
        }

        #region Properties
        public int getId()
        {
            return this.id;
        }
        public void setId(int value)
        {
            this.id = value;
        }
        public int getUserId()
        {
            return this.userId;
        }
        public void setUserId(int value)
        {
            this.userId = value;
        }
        public DateTime getDate()
        {
            return this.date;
        }
        public void setDate(DateTime value)
        {
            this.date = value;
        }
        #endregion

    }

    public class InventoryItem
    {
        private int furnitureId; //furnitureId field
        private int count; //quantity field

        public InventoryItem(int furnitureId, int count)
        {
            this.furnitureId = furnitureId;
            this.count = count;
        }

        public int getFurnitureId()
        {
            return this.furnitureId;
        }
        public void setFurnitureId(int value)
        {
            this.furnitureId = value;
        }
        public int getCount()
        {
            return this.count;
        }
        public void setCount(int value)
        {
            this.count = value;
        }
    }

    
}
