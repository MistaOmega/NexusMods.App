using System.Xml;

namespace NexusMods.Games.Larian.BaldursGate3.Utils.PakParsing;

public static class LsxXmlFormat
{
    
    
    /// <summary>
    /// Pak metadata, containing the module short description and its dependencies.
    /// </summary>
    public struct MetaFileData
    {
        public ModuleShortDesc ModuleShortDesc;
        public ModuleShortDesc[] Dependencies;
    }
    
    
    /// <summary>
    /// Pak module short description.
    /// </summary>
    public struct ModuleShortDesc
    {
        public string Folder;
        public string Name;
        public string PublishHandle;
        public string Version;
        public string Uuid;
        public string Md5;
    }
    
    
    /// <summary>
    /// Serializes a <see cref="ModuleShortDesc"/> to an LSX `ModuleShortDesc` xml element string.
    /// </summary>
    public static string SerializeModuleShortDesc(ModuleShortDesc moduleShortDesc)
    {
        using var stringWriter = new StringWriter();
        using (var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings { Indent = true, OmitXmlDeclaration = true}))
        {
            xmlWriter.WriteStartElement("node");
            xmlWriter.WriteAttributeString("id", "ModuleShortDesc");

            WriteAttribute(xmlWriter, "Folder", "LSString", moduleShortDesc.Folder);
            WriteAttribute(xmlWriter, "MD5", "LSString", moduleShortDesc.Md5);
            WriteAttribute(xmlWriter, "Name", "LSString", moduleShortDesc.Name);
            WriteAttribute(xmlWriter, "PublishHandle", "uint64", moduleShortDesc.PublishHandle);
            WriteAttribute(xmlWriter, "UUID", "guid", moduleShortDesc.Uuid);
            WriteAttribute(xmlWriter, "Version64", "int64", moduleShortDesc.Version);

            xmlWriter.WriteEndElement();
        }

        return stringWriter.ToString();
        
        static void WriteAttribute(XmlWriter xmlWriter, string id, string type, string value)
        {
            xmlWriter.WriteStartElement("attribute");
            xmlWriter.WriteAttributeString("id", id);
            xmlWriter.WriteAttributeString("type", type);
            xmlWriter.WriteAttributeString("value", value);
            xmlWriter.WriteEndElement();
        }
    }


    /// <summary>
    /// Collection of modules that should be ignored when parsing dependencies.
    /// These are mostly vanilla pak files, so not relevant for dependencies between mods. 
    /// </summary>
    public static readonly ModuleShortDesc[] DependencyModulesToIgnore =
    [
        new()
        {
            // Vanilla pak file
            Name = "Gustav",
            Uuid = "991c9c7a-fb80-40cb-8f0d-b92d4e80e9b1",
        },
        new()
        {
            // Vanilla pak file
            Name = "DiceSet_01",
            Uuid = "e842840a-2449-588c-b0c4-22122cfce31b",
        },
        new()
        {
            // Vanilla pak file
            Name = "GustavDev",
            Uuid = "28ac9ce2-2aba-8cda-b3b5-6e922f71b6b8",
        },
        new()
        {
            // Vanilla pak file
            Name = "DiceSet_02",
            Uuid = "b176a0ac-d79f-ed9d-5a87-5c2c80874e10",
        },
        new()
        {
            // Vanilla pak file
            Name = "DiceSet_03",
            Uuid = "e0a4d990-7b9b-8fa9-d7c6-04017c6cf5b1",
        },
        new()
        {
            // Vanilla pak file
            Name = "DiceSet_04",
            Uuid = "77a2155f-4b35-4f0c-e7ff-4338f91426a4",
        },
        new()
        {
            // Vanilla pak file
            Name = "Shared",
            Uuid = "ed539163-bb70-431b-96a7-f5b2eda5376b",
        },
        new()
        {
            // Vanilla pak file
            Name = "SharedDev",
            Uuid = "3d0c5ff8-c95d-c907-ff3e-34b204f1c630",
        },
        new()
        {
            // Vanilla pak file
            Name = "FW3",
            Uuid = "e5c9077e-1fca-4f24-b55d-464f512c98a8",
        },
        new()
        {
            // Vanilla pak file
            Name = "Engine",
            Uuid = "9dff4c3b-fda7-43de-a763-ce1383039999",
        },
        new()
        {
            // Vanilla pak file
            Name = "DiceSet_06",
            Uuid = "ee4989eb-aab8-968f-8674-812ea2f4bfd7",
        },
        new()
        {
            // Vanilla pak file
            Name = "Honour",
            Uuid = "b77b6210-ac50-4cb1-a3d5-5702fb9c744c",
        },
        new()
        {
            // Vanilla pak file
            Name = "ModBrowser",
            Uuid = "ee5a55ff-eb38-0b27-c5b0-f358dc306d34",
        },
        new()
        {
            // Vanilla pak file
            Name = "MainUI",
            Uuid = "630daa32-70f8-3da5-41b9-154fe8410236",
        },
    ];
    
    /// <summary>
    /// Dependency Name values to ignore, based on the <see cref="DependencyModulesToIgnore"/> collection.
    /// </summary>
    public static readonly string[] DependencyNamesToIgnore = DependencyModulesToIgnore.Select(m => m.Name).ToArray();
    
    /// <summary>
    /// Dependency Uuid values to ignore, based on the <see cref="DependencyModulesToIgnore"/> collection.
    /// </summary>
    public static readonly string[] DependencyUuidsToIgnore = DependencyModulesToIgnore.Select(m => m.Uuid).ToArray();

}
