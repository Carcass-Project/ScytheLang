using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Scythe
{
    public class ScytheProject
    {
        public string ProjectName { get; internal set; }
        public string ProjectVersion { get; internal set;}
        public float ScytheVersion { get; internal set; }

        public string[] Packages { get; internal set; }

        public string Target { get; internal set; }

        public void GenerateProjectFile()
        {
            var x = File.Create(ProjectName + ".syproj");
            x.Close();
            TextWriter _textw = new StreamWriter(new FileStream(ProjectName+".syproj", FileMode.Open));
            var _Writer = new JsonTextWriter(_textw);
            _Writer.Formatting = Formatting.Indented;

            _Writer.WritePropertyName("ProjectName");
            _Writer.WriteValue(ProjectName);

            _Writer.WritePropertyName("Version");
            _Writer.WriteValue(ProjectVersion);

            _Writer.WritePropertyName("LanguageVersion");
            _Writer.WriteValue(ScytheVersion);

            _Writer.WritePropertyName("TargetArch");
            _Writer.WriteValue(Target);

            _Writer.WritePropertyName("Packages");
            _Writer.WriteStartArray();
            foreach(var package in Packages)
            {
                _Writer.WriteStartObject();
                _Writer.WritePropertyName("Package");
                _Writer.WriteValue(package);
                _Writer.WriteEndObject();
            }
            _Writer.WriteEndArray();

            _Writer.Close();
        }

        public ScytheProject(string projectName, string projectVersion, float scytheVersion, string[] packages, string target)
        {
            ProjectName = projectName;
            ProjectVersion = projectVersion;
            ScytheVersion = scytheVersion;
            Packages = packages;
            Target = target;
        }
    }
}
