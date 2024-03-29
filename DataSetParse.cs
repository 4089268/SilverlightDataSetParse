using System.ComponentModel;
using System.Data;
using System.Xml;

namespace SilverlightDataSetParse {
    public class DataSetParse {

        private DataSet? myDataSet;
        
        private XmlReaderSettings xmlReaderSettings;
        public DataSetParse(){
            xmlReaderSettings = new XmlReaderSettings{
                Async = true
            };
        }

        public DataSet Parse(string xmlContent){
            
            var dataSet = GenerarDataSet(xmlContent);

            // Extraer datos
            foreach(DataTable table in dataSet.Tables){
                ExtrarDatosTabla(table, xmlContent);
            }

            return dataSet;

        }

        private void ExtrarDatosTabla(DataTable table, string xmlContent)
        {
            var tableName = table.TableName;
            var columns = table.Columns;

            // Extrar filas xml
            IEnumerable<string> xmlTableContent = ExtrarDatosXmlTabla(xmlContent, tableName);

            // Extraer valores columna
            foreach(var xmlRow in xmlTableContent){
                var newRow = table.NewRow();
                foreach(DataColumn column in columns){
                    try{
                        var newValue = ExtrarValorColumn(xmlRow, column);
                        if(newValue  != null){
                            newRow[column.ColumnName] = newValue;
                        }
                    }catch(Exception err){
                        Console.WriteLine($"Error al extrar el valor TableName:{tableName} ColumnName:{column.ColumnName}  Row: {xmlRow} " + err.Message + "");
                        throw new Exception();
                    }
                }
                table.Rows.Add(newRow);
            }
        }

        private object? ExtrarValorColumn(string xmlRow, DataColumn column)
        {
            object? velueColum = null;
            using(StringReader stringReader = new StringReader(xmlRow)){
                using(var xmlReader = XmlReader.Create(stringReader, xmlReaderSettings)){
                    while(xmlReader.Read()){
                        if(xmlReader.NodeType == XmlNodeType.Element){
                            if(xmlReader.Name.ToLower() == column.ColumnName.ToLower()){
                                var contentString = xmlReader.ReadInnerXmlAsync().GetAwaiter().GetResult();
                                var converter = TypeDescriptor.GetConverter(column.DataType);
                                velueColum = converter.ConvertFrom( null, new System.Globalization.CultureInfo("es-MX"), contentString);
                                break;
                            }
                        }
                    }
                }
            }
            return velueColum;
        }

        private IEnumerable<string> ExtrarDatosXmlTabla(string xmlContent, string tableName)
        {
            var xmlTableContent = new List<string>();
            
            using(var stringReader = new StringReader(xmlContent)){
                using(var xmlReader = XmlReader.Create(stringReader, this.xmlReaderSettings)){
                    while(xmlReader.Read()){
                        if(xmlReader.NodeType == XmlNodeType.Element){
                            if(xmlReader.Name == tableName){
                                xmlTableContent.Add( xmlReader.ReadOuterXmlAsync().GetAwaiter().GetResult());
                            }
                        }
                    }
                }
            }
            return xmlTableContent;
        }

        public DataSet GenerarDataSet(string xmlSchema){
            var myDataSet = new DataSet();

            // Obtener esquema del data set
            String xmlDataSetSchena = ExtractDataSetSchema(xmlSchema).GetAwaiter().GetResult();

            // Obtener nombre
            using(StringReader stringReader = new StringReader(xmlDataSetSchena)){
                using(var xmlReader = XmlReader.Create(stringReader, xmlReaderSettings)){
                    while(xmlReader.Read()){
                        if(xmlReader.NodeType == XmlNodeType.Element){
                            if(xmlReader.GetAttribute("msdata:IsDataSet") != null){
                                myDataSet.DataSetName = xmlReader.GetAttribute("name")?? new String(Guid.NewGuid().ToString().Replace("-","").Take(10).ToArray());
                            }
                        }
                    }
                }
            }

            // Generar Tablas
            var tablasGeneradas = GenerarDataTables(xmlDataSetSchena);

            myDataSet.Tables.AddRange(tablasGeneradas.ToArray());
            return myDataSet;
        }
        private IEnumerable<DataTable> GenerarDataTables(string xmlSchema){
            var tables = new List<DataTable>();

            // Extrar esquemas de los data tables
            IEnumerable<string> tableSchamas = ExtractTablesSchemas(xmlSchema).GetAwaiter().GetResult();
            foreach(var tableSchama in tableSchamas){

                // Generar dataTables por cada esquema encontrado
                var newTable =   GenerarDataTable(tableSchama);
                tables.Add(newTable);
            }
            return tables;
        }
        
        private async Task<string> ExtractDataSetSchema(string xmlContent){
            string xmlSchema = string.Empty;

            var dataSetName = "";

            XmlReaderSettings settings = new XmlReaderSettings(){ Async = true };
            using(var stream = new StringReader(xmlContent)){
                using (XmlReader reader = XmlReader.Create(stream, xmlReaderSettings))
                {
                    while(reader.Read()){
                        if(reader.NodeType == XmlNodeType.Element){
                            if(reader.GetAttribute("msdata:IsDataSet") != null){
                                dataSetName = reader.GetAttribute("name")??"";
                                xmlSchema = await reader.ReadOuterXmlAsync();
                                break;
                            }
                        }
                    }
                    if(string.IsNullOrEmpty(xmlSchema)){
                        throw new Exception(" Esquema DataSet no encontrado");
                    }
                }
            }
            return xmlSchema;
        }
        private async Task<IEnumerable<string>> ExtractTablesSchemas(string xmlString){
            var  xmlTables = new List<string>();
            using(var stringReader = new StringReader(xmlString)){
                using(var reader = XmlReader.Create(stringReader, this.xmlReaderSettings)){
                    while(reader.Read()){
                        if(reader.NodeType == XmlNodeType.Element){
                            if(reader.Name == "xs:element"){
                                if(reader.GetAttribute("msdata:IsDataSet") == null && reader.GetAttribute("type") == null){
                                    xmlTables.Add( await reader.ReadOuterXmlAsync());
                                }
                            }
                        }
                    }
                }
            }
            return xmlTables;
        }
        private DataTable GenerarDataTable(string xmlString){
            var dataTable = new DataTable();
            using(var stringReader = new StringReader(xmlString)){
                using(var reader = XmlReader.Create(stringReader, xmlReaderSettings)){
                    while(reader.Read()){
                        if(reader.NodeType == XmlNodeType.Element){
                            if(reader.Name == "xs:element"){

                                if(reader.GetAttribute("type") == null){

                                    // Obtener el nombre de la tabla
                                    dataTable.TableName = reader.GetAttribute("name");
                                }else{

                                    // Genearar dataColumn
                                    string columnName = reader.GetAttribute("name")?? new String(Guid.NewGuid().ToString().Replace("-","").Take(6).ToArray());
                                    string columnTypeName = reader.GetAttribute("type")??"string";

                                    var typeColumn = GetColumnType(columnTypeName);
                                    if(typeColumn != null){
                                        var newDataColumn = new DataColumn(columnName, typeColumn);
                                        dataTable.Columns.Add(newDataColumn);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return dataTable;
        }
        private Type? GetColumnType(string typeName){
            var cTypeName = typeName.Replace("xs:","");
            Type? myType = null;
            
            switch(cTypeName.ToLower()){
                case "int":
                    myType = typeof(int);
                    break;
                case "short":
                    myType = typeof(short);
                    break;
                case "long":
                    myType = typeof(long);
                    break;
                default:
                    myType = Type.GetType($"system.{cTypeName}", true, true);
                    break;
            }
            return myType;
        }

    }
}