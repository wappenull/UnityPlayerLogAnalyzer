using static UnityPlayerLogAnalyzer.SourceFile;

namespace UnityPlayerLogAnalyzer
{
    /// <summary>
    /// This captures exception pattern.
    /// </summary>
    static class CaptureMethod2
    {
        /// <summary>
        /// Check if this line looks like part of exception report?
        /// </summary>
        public static bool Detect( string lineText )
        {
            // Exception could be in 2 form, real exception or printed
            // It could follow by more Callstack line after at statements
            // They usually did not include any Debug:Log pattern so it will not detected by method 1
            //
            // <start with one newline>
            // ExceptionHead
            //   at Callstack
            //   at Callstack
            // Callstack
            // <ends with double newline>
            const string ExceptionPattern = "  at ";
            return lineText.StartsWith( ExceptionPattern );
        }

        public static void CaptureLogLineAround( int atLine, SourceFile source, LogLine ll )
        {
            // Type is always error for exception
            ll.logType = LogLine.LogType.Error;

            // Find head/tail, search up until we find double blank line
            int head = source.SearchForDoubleNewLine( atLine, SearchDirection.Up );
            int tail = source.SearchForDoubleNewLine( atLine, SearchDirection.Down );

            CaptureMethodCommon.FastForwardLineIfThereIsUnityInternalLogLine( ref head, tail, source );

            // Callstack start is line with first "  at " keyword. which is this line from Detect
            int callStackStart = atLine;

            ll.message = source.CaptureTextFromToLine( head, callStackStart - 1 );
            ll.callstack = source.CaptureTextFromToLine( atLine, tail );

            ll.startFromSourceLine = head;
            ll.endAtSourceLine = tail;
        }
    }
}
