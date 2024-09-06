using System.Text.RegularExpressions;
using Fls.AcesysConversion.Common.Entities;
using Fls.AcesysConversion.Common.Enums;

namespace Fls.AcesysConversion.PLC.Siemens.Components
{
    public abstract partial class SiemensCollection : SiemensItemBase
    {
        public SiemensCollection(string prefix, string localname, string nsURI) : base(prefix, localname, nsURI)
        {
        }

        public string? this[int index] => ChildItems.ElementAtOrDefault(index);

        public new string? this[string id]
        {
            get
            {
                string? item = ChildItems.FirstOrDefault(x => x.Contains(id, StringComparison.InvariantCultureIgnoreCase));
                return item;
            }
        }

        public bool Exist(string newName)
        {
            return ChildItems.Any(x => x.Contains(newName, StringComparison.InvariantCultureIgnoreCase));
        }

        public bool Remove(int index)
        {
            if (index >= 0 && index < ChildItems.Count)
            {
                ChildItems.RemoveAt(index);
                return true;
            }
            return false;
        }

        public bool Remove(string id, bool isAddMessage = false, string operation = "", string optionalMessage = "")
        {
            string? item = ChildItems.FirstOrDefault(x => x.Contains(id, StringComparison.InvariantCultureIgnoreCase));
            if (item != null)
            {
                ChildItems.Remove(item);

                if (isAddMessage)
                {
                    AddUserMessage(null, item, UserMessageTypes.Information, "Remove", operation, optionalMessage);
                }

                return true;
            }
            return false;
        }

        public bool RemoveByPattern(string pattern, bool isAddMessage = false, string operation = "", string optionalMessage = "")
        {
            string? item = ChildItems.FirstOrDefault(x => Regex.IsMatch(x, pattern));
            if (item != null)
            {
                ChildItems.Remove(item);

                if (isAddMessage)
                {
                    AddUserMessage(null, item, UserMessageTypes.Information, "Remove", operation, optionalMessage);
                }

                return true;
            }
            return false;
        }

        public int Count => ChildItems.Count;

        public void Clear()
        {
            ChildItems.Clear();
        }

        public virtual bool Add(string newName, string awlStdStructure, string replacementType)
        {
            if (Exist(newName))
            {
                _ = Remove(newName);
            }

            string newItem = RemoveTabAndNewLineRegex().Replace(awlStdStructure, "");
            ChildItems.Add(newItem);

            if (!Exist(newName))
            {
                AddUserMessage(null, newItem, UserMessageTypes.Information, "Add", replacementType, "");
            }
            else
            {
                AddUserMessage(null, newItem, UserMessageTypes.Information, "Add", replacementType);
            }
            return true;
        }

        public void AddByContent(string content)
        {
            ChildItems.Add(content);
        }

        public static void AddUserMessage(string? newNode, string? originalNode, UserMessageTypes umt, string operation, string replacementType, string message = "")
        {
            string newNodeName = newNode ?? "Unknown";

            UserMessage msg = new UserMessage(
                -1, // No UI identifier in this context
                -1,
                newNodeName,
                newNodeName,
                umt,
                operation,
                replacementType,
                message
            );

            // Announce the message (to be implemented)
            // project.Announce(msg);
        }

        [GeneratedRegex("\\t|\\n|\\r")]
        private static partial Regex RemoveTabAndNewLineRegex();

        protected List<string> ChildItems { get; } = new();
    }
}