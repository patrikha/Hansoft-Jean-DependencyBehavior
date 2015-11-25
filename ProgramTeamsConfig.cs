using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Hansoft.Jean.Behavior.DependencyBehavior
{
    public class ProgramTeamsConfig
    {
        private static Dictionary<string, HashSet<string>> programs = null;

        public static bool IsTeamInProgram(string program, string team)
        {
            if (programs == null)
                ProgramTeamsConfig.ReadConfig();

            HashSet<string> teams = null;
            if (!programs.TryGetValue(program.ToLower(), out teams))
                return false;
            return teams.Contains(team.ToLower());
        }

        public static void ReadConfig()
        {
            programs = new Dictionary<string, HashSet<string>>();
            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            FileInfo fInfo = new FileInfo(Path.Combine(currentDirectory, "ProgramTeams.xml"));
            if (!fInfo.Exists)
                throw new ArgumentException("Could not find settings file " + fInfo.FullName);

            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(fInfo.FullName);
            XmlElement documentElement = xmlDocument.DocumentElement;
            if (documentElement.Name != "ProgramTeams")
                throw new FormatException("The root element of the settings file must be of type ProgramTeams, got " + documentElement.Name);

            XmlNodeList programNodes = documentElement.ChildNodes;

            foreach (XmlNode program in programNodes)
            {
                if (program.NodeType != XmlNodeType.Element)
                    continue;

                XmlElement programElement = (XmlElement)program;
                string name = "";
                foreach (XmlAttribute attr in programElement.Attributes)
                {
                    if (attr.Name.ToLower() == "name")
                        name = attr.Value.ToLower();
                }
                var teams = new HashSet<string>();
                programs.Add(name.ToLower(), teams);
                foreach (XmlNode team in program.ChildNodes)
                {
                    if (team.NodeType != XmlNodeType.Element)
                        continue;

                    XmlElement teamElement = (XmlElement)team;
                    foreach (XmlAttribute attr in teamElement.Attributes)
                    {
                        if (attr.Name.ToLower() == "name")
                            teams.Add(attr.Value.ToLower());
                    }
                }
            }
        }
    }
}
