// See https://aka.ms/new-console-template for more information
using System.Data;
using SilverlightDataSetParse;

Console.WriteLine("Hello World!");

var dataSetParse = new DataSetParse();

// Leer archivo
//string urlFile = System.IO.Directory.GetCurrentDirectory() + "/xmlSamples/datasetSample.xml";
string urlFile = System.IO.Directory.GetCurrentDirectory() + "/xmlSamples/datasetSampleMultiTable.xml";
var fileContentString = File.ReadAllText(urlFile);

var dataSet = dataSetParse.Parse(fileContentString);


Console.WriteLine("(-) DataSet Name: " + dataSet.DataSetName);
foreach(DataTable table in dataSet.Tables){
    Console.WriteLine($"\t(-) DataTable Name: {table.TableName}");
    foreach( DataColumn column in table.Columns ){
        Console.WriteLine($"\t\t(-) DataColumn Name:{column.ColumnName}  Type:{column.DataType.Name}");
    }

    Console.WriteLine($"\t(-) Total Rows: {table.Rows.Count}");


}
     