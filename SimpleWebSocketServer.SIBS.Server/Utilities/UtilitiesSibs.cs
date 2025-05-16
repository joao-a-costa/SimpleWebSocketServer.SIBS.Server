using System.Reflection;
using System.ComponentModel;
using static SimpleWebSocketServer.SIBS.Server.Enums.Enums;

namespace SimpleWebSocketServer.SIBS.Server.Utilities
{
    public static class UtilitiesSibs
    {
        public static string GetEnumDescription(TerminalCommandOptions value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attribute = (DescriptionAttribute)field.GetCustomAttribute(typeof(DescriptionAttribute));
            return attribute == null ? value.ToString() : attribute.Description;
        }
    }
}
