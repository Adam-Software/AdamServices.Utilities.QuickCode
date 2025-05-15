namespace QuickCode.Services.Interfaces.JsonModel
{
    public class ReceivedMessage
    {
        /// <summary>
        /// debug_source_code || source_code || control_characters
        /// </summary>
        public string MessageType { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;

        public string ControlCharacters { get; set; } = string.Empty;
    }
}
