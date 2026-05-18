namespace xyLogger.Helpers
{
    /// <summary>
    /// Outsourcing the output from thew Logger to reduce dependencies and give universal access
    /// </summary>
    public static class xyOutput
    {
        /// <summary>
        /// Writes a formatted message to the console"/>.
        /// </summary>
        /// <param name="formattedMessage">The fully formatted log string to output.</param>
        /// </param>
        public static void Output(string formattedMessage)
        {
            Console.WriteLine(formattedMessage);
            Console.Out.Flush();
        }

        /// <summary>
        /// Asynchronously writes a formatted message to the console/>.
        /// </summary>
        /// <param name="formattedMessage">The fully formatted log string to output.</param>
        /// </param>
        public static async Task OutputAsync(string formattedMessage)
        {
            await Console.Out.WriteLineAsync(formattedMessage);
            await Console.Out.FlushAsync();
        }

        public static void OutputError(string message)
        {
            Console.Error.WriteLine(message);
            Console.Error.Flush();
        }
        public static async Task OutputErrorAsync(string message)
        {
            await Console.Error.WriteLineAsync(message);
            await Console.Error.FlushAsync();
        }




    }
}
