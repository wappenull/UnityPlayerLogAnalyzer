using System;
using System.Windows.Forms;
using static UnityPlayerLogAnalyzer.SourceFile;

namespace UnityPlayerLogAnalyzer
{
    /// <summary>
    /// This capture all logs including warning, errors. But not exceptions.
    /// </summary>
    static class CaptureMethod1
    {
        public const string LogKeyword = "UnityEngine.Debug:Log";
        const string CallStackStart_2019_4_Early = "UnityEngine.DebugLogHandler:Internal_Log";
        const string CallStackStart_2019_4_26 = "StackTraceUtility:ExtractStackTrace";

        /// <summary>
        /// This will only warn once.
        /// </summary>
        static bool s_CallStackStartFaultWarned;

        public static bool Detect( string lineText )
        {
            return lineText.StartsWith( LogKeyword );
        }

        public static void CaptureLogLineAround( int atLine, SourceFile source, LogLine ll )
        {
            string logType = source[atLine].Replace( LogKeyword, "" );
            if( logType.StartsWith( "Error" ) )
                ll.logType = LogLine.LogType.Error;
            else if( logType.StartsWith( "Warning" ) )
                ll.logType = LogLine.LogType.Warning;
            else
                ll.logType = LogLine.LogType.Log;

            // Find head/tail, search up until we find double blank line
            int headLine = source.SearchForDoubleNewLine( atLine, SearchDirection.Up );
            int tailLine = source.SearchForDoubleNewLine( atLine, SearchDirection.Down );
            
            CaptureMethodCommon.FastForwardLineIfThereIsUnityInternalLogLine( ref headLine, tailLine, source );

            // 2019.4.26 log file uses another identifier
            int callStackStart = source.SearchForLineContaining( atLine, CallStackStart_2019_4_Early, SearchDirection.Up, limit: headLine );
            if( callStackStart < 0 )
                callStackStart = source.SearchForLineContaining( atLine, CallStackStart_2019_4_26, SearchDirection.Up, limit: headLine );

            if( callStackStart < 0 )
            {
                callStackStart = headLine+1;// Default to one line message
                if( !s_CallStackStartFaultWarned )
                {
                    s_CallStackStartFaultWarned = true;
                    MessageBox.Show( 
                        $"A pattern form 'callstack start' could not be found.\n" +
                        $"Like '{CallStackStart_2019_4_Early}' or '{CallStackStart_2019_4_26}'\n" +
                        $"Message and call stack might not fully extract in this run.\n" +
                        $"(Unity callstack function can change over versions, modify code to support more of them!)\n" +
                        $"This will only warn once." );
                }
            }
            
            // Usually message is just 1 line above Internal_Log
            ll.message = source.CaptureTextFromToLine( headLine, callStackStart - 1 );
            ll.callstack = source.CaptureTextFromToLine( callStackStart + 1, tailLine );

            ll.startFromSourceLine = headLine;
            ll.endAtSourceLine = tailLine;
        }
    }

    static class CaptureMethodCommon
    {
        /// <summary>
        /// Fast forward 'head' if there is predetermined Unity's internal logging line.
        /// But cannot goes beyond tail.
        /// </summary>
        public static void FastForwardLineIfThereIsUnityInternalLogLine( ref int head, int tail, SourceFile source )
        {
            while( head < source.Length && _IsUnityInternalLine( source[head] ) )
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
