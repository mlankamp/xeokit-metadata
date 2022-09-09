using System.Text.Json;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

if (args.Length < 2)
{
    Console.WriteLine("Please specify the path to the IFC and the output json.");
    Console.WriteLine(@"
          Usage:
          
          $ metadata /path/to/some.ifc /path/to/output.json

    ");
    Environment.Exit(1);
}


var ifcPath = args[0];
if (!File.Exists(ifcPath))
{
    Console.WriteLine("The IFC file does not exists at path: {0}", ifcPath);
    Environment.Exit(1);
}

var jsonPath = args[1];

using var model = IfcStore.Open(ifcPath);
using var fileStream = File.Create(jsonPath);
using Utf8JsonWriter writer = new Utf8JsonWriter(fileStream, new JsonWriterOptions() { Indented = false });

var project = model.Instances.FirstOrDefault<IIfcProject>();
var header = model.Header;

writer.WriteStartObject();
writer.WriteString("id", project.Name);
writer.WriteString("projectId", project.GlobalId.ToString());
writer.WriteString("author", string.Join(';', header.FileName.AuthorName));
writer.WriteString("createdAt", header.TimeStamp);
writer.WriteString("schema", header.SchemaVersion);
writer.WriteString("creatingApplication", header.FileName.PreprocessorVersion);
writer.WriteStartArray("metaObjects");

ExtractHierarchy(project, writer);

writer.WriteEndArray();
writer.WriteEndObject();

await writer.FlushAsync();



static void ExtractHierarchy(IIfcObjectDefinition objectDefinition, Utf8JsonWriter writer, string? parentId = null)
{
    WriteMetaObject(objectDefinition, writer, parentId);

    var spatialElement = objectDefinition as IIfcSpatialStructureElement;
    if (spatialElement != null)
    {
        var containedElements = spatialElement.ContainsElements.SelectMany(rel => rel.RelatedElements);
        foreach (var element in containedElements)
        {
            WriteMetaObject(element, writer, spatialElement.GlobalId);

            extractRelatedObjects(element, writer, element.GlobalId);
        }
    }

    extractRelatedObjects(objectDefinition, writer, objectDefinition.GlobalId);
}

static void extractRelatedObjects(IIfcObjectDefinition objectDefinition, Utf8JsonWriter writer, string parentObjId)
{
    var relatedObjects = objectDefinition.IsDecomposedBy.SelectMany(r => r.RelatedObjects);
    foreach (var item in relatedObjects)
    {
        ExtractHierarchy(item, writer, parentObjId);
    }
}

static void WriteMetaObject(IIfcObjectDefinition objectDefinition, Utf8JsonWriter writer, string? parentId = null)
{
    var type = objectDefinition.GetType().Name;
    var name = string.IsNullOrEmpty(objectDefinition.Name) ? type.Replace("Ifc", string.Empty) : objectDefinition.Name.ToString();

    writer.WriteStartObject();
    writer.WriteString("id", objectDefinition.GlobalId.ToString());
    writer.WriteString("name", name);
    writer.WriteString("type", type);
    writer.WriteString("parent", parentId);
    if (objectDefinition is IIfcBuildingStorey story && story.Elevation.HasValue)
    {
        var elevation = (double)story.Elevation.Value.Value;
        writer.WriteString("elevation", elevation.ToString("#.###"));
    }
    writer.WriteEndObject();
}