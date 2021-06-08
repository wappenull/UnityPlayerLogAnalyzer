namespace UnityPlayerLogAnalyzer
{
    public static class LineUtil
    {
        /// <summary>
        /// The range from-to is inclusive max.
        /// </summary>
        public static string CaptureTextFromToLine( int from, int to, string[] allLines )
        {
            return string.Join( "\n", allLines, from, to - from + 1 );
        }

        /// <summary>
        /// Returns line number that start the double new line, line which has first new line of the double.
        /// If search is going up, it returns line which as second newline.
        /// </summary>
        /// <remarks>
        /// If search were to start anywhere around line 1-4:
        /// 0 
        /// 1 text - searching up will return this line
        /// 2 text
        /// 3 text - searching down will return this line
        /// 4 
        /// </remarks>
        public static int SearchForDoubleNewLine( int i, string[] allLines, SearchDirection dir )
        {
            if( dir == SearchDirection.Up )
            {
                while( i-- > 0 )
                {
                    if( string.IsNullOrWhiteSpace( allLines[i] ) ) // As all lines are already splitted, if there is empty line that's mean there is double newline there
                        return i + 1;
                }
            }
            else
            {
                while( ++i < allLines.Length )
                {
                    if( string.IsNullOrWhiteSpace( allLines[i] ) )
                        return i - 1;
                }
            }
            return i;
        }

        public static int SearchForLineContaining( int i, string[] allLines, string text, SearchDirection dir )
        {
            if( dir == SearchDirection.Up )
            {
                while( i-- > 0 )
                {
                    if( allLines[i].Contains( text ) )
                        return i;
                }
            }
            else
            {
                while( ++i < allLines.Length )
                {
                    if( allLines[i].Contains( text ) )
                        return i;
                }
            }
            return i;
        }

        public enum SearchDirection
        {
            Up, Down
        }
    }
}
