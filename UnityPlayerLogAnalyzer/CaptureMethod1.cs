using System;
using static UnityPlayerLogAnalyzer.LineUtil;

namespace UnityPlayerLogAnalyzer
{
    static class CaptureMethod1
    {
        public const string LogKeyword = "UnityEngine.Debug:Log";
        const string CallStackStart = "UnityEngine.DebugLogHandler:Internal_Log";

        public static bool Detect( string lineText )
        {
            return lineText.StartsWith( LogKeyword );
        }

        public static void CaptureLogLineAround( int atLine, string[] allLines, LogLine ll )
        {
            string logType = allLines[atLine].Replace( LogKeyword, "" );
            if( logType.StartsWith( "Error" ) )
                ll.type = LogLine.LogType.Error;
            else if( logType.StartsWith( "Warning" ) )
                ll.type = LogLine.LogType.Warning;
            else
                ll.type = LogLine.LogType.Log;

            // Find head/tail, search up until we find double blank line
            int head = SearchForDoubleNewLine( atLine, allLines, SearchDirection.Up );
            int tail = SearchForDoubleNewLine( atLine, allLines, SearchDirection.Down );
            
            CaptureMethodCommon.FastForwardLineIfThereIsUnityInternalLogLine( ref head, tail, allLines );

            int callStackStart = SearchForLineBeginWith( atLine, allLines, CallStackStart, SearchDirection.Up );

            // Usually message is just 1 line above Internal_Log
            ll.message = CaptureTextFromToLine( head, callStackStart - 1, allLines );
            ll.callstack = CaptureTextFromToLine( callStackStart + 1, tail, allLines );

            ll.startFromSourceLine = head;
            ll.endAtSourceLine = tail;
        }
    }

    static class CaptureMethodCommon
    {
        /// <summary>
        /// Fast forward 'head' if there is predetermined Unity's internal logging line.
        /// But cannot goes beyond tail.
        /// </summary>
        public static void FastForwardLineIfThereIsUnityInternalLogLine( ref int head, int tail, string[] allLines )
        {
            while( head < allLines.Length && _IsUnityInternalLine( allLines[head] ) )
            {
                if( head + 1 >= tail ) // Cannot goes anymore than that
                    return;

                head++;
            }
            
        }

        private static bool _IsUnityInternalLine( string v )
        {
            // There are large number of it, let's pick something that we seen commonly and does not ended with double newlines
            // We are fine with system messages those end with double new line
            if( v.StartsWith( "UnloadTime: " ) ) return true;
            if( v.Contains( "Unused Serialized files (Serialized files now" ) ) return true;

            return false;
        }
    }
}
