using static UnityPlayerLogAnalyzer.LineUtil;

namespace UnityPlayerLogAnalyzer
{
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

        public static void CaptureLogLineAround( int atLine, string[] allLines, LogLine ll )
        {
            // Type is always error for exception
            ll.type = LogLine.LogType.Error;

            // Find head/tail, search up until we find double blank line
            int head = SearchForDoubleNewLine( atLine, allLines, SearchDirection.Up );
            int tail = SearchForDoubleNewLine( atLine, allLines, SearchDirection.Down );

            CaptureMethodCommon.FastForwardLineIfThereIsUnityInternalLogLine( ref head, tail, allLines );

            // Callstack start is line with first "  at " keyword. which is this line from Detect
            int callStackStart = atLine;

            ll.message = CaptureTextFromToLine( head, callStackStart - 1, allLines );
            ll.callstack = CaptureTextFromToLine( atLine, tail, allLines );

            ll.startFromSourceLine = head;
            ll.endAtSourceLine = tail;
        }
    }
}
