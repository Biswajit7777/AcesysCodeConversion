using Fls.AcesysConversion.Common;
using Fls.AcesysConversion.Common.Enums;
using Fls.AcesysConversion.Helpers.Database;
using System.Xml;

namespace Fls.AcesysConversion.PLC.Rockwell.Components.Tags
{
    public class L5XTags : L5XCollection
    {
        public RockwellL5XProject Project { get; private set; }
        public int MessageboardReference { get; private set; }

        public L5XTags(string prefix, string localName, string namespaceURI, XmlDocument doc, int seq)
            : base(prefix, localName, namespaceURI, doc)
        {
            MessageboardReference = seq;
            Project = (RockwellL5XProject)OwnerDocument;
        }

        public override void UpgradeVersion(L5XCollection? original, RockwellUpgradeOptions options, IProgress<string> progress)
        {
            var dbh = DbHelper.Instance.GetDbHelper((int)options.FromVersion, (int)options.ToVersion, Project.Manufacturer);
            var originalCollection = original as L5XTags;
            UpgradeEngine? processor = null;

            if (originalCollection != null)
            {
                processor = new UpgradeEngineFactory()
                    .SetCollections(this, originalCollection, Project)
                    .SetUpgradeProperties(options.FromVersion, options.ToVersion, Project.Manufacturer, typeof(L5XTags))
                    .Create();
            }

            if (processor != null)
            {
                UpgradeManager.ProcessUpgrade(processor, dbh, options, progress);
            }
        }

        public L5XTag? Item(int index)
        {
            return (L5XTag?)base[index];
        }

        public L5XTag? Item(string id)
        {
            return (L5XTag?)base[id];
        }

        // Method to check for existing tags by name before appending a child
        public L5XTag? TryGetTagByName(string name)
        {
            return (L5XTag?)base.SelectSingleNode($"Tag[@Name='{name}']");
        }

        // Override AppendChild to prevent duplicates
        public override XmlNode AppendChild(XmlNode newChild)
        {
            // Check if the newChild is an XmlElement and if it has a "Name" attribute
            if (newChild is XmlElement newTagElement && newTagElement.Name == "Tag")
            {
                // Retrieve the Name attribute from the new tag
                string tagName = newTagElement.GetAttribute("Name");

                // Check if a tag with the same name already exists
                if (TryGetTagByName(tagName) != null)
                {
                    // Log a warning and do not append the duplicate tag
                    AddUserMessage(Project, null, null, UserMessageTypes.Warning, $"Tag '{tagName}' already exists and will not be added.", tagName);
                    return null;  // Prevent the tag from being appended
                }
            }

            // If no duplicate found, proceed to append the tag
            XmlNode appendedNode = base.AppendChild(newChild);

            // Log the successful addition of the tag
            if (newChild is XmlElement)
            {
                string name = ((XmlElement)newChild).GetAttribute("Name");                
            }

            return appendedNode;
        }
    }
}