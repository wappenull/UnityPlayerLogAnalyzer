using System.Collections.Generic;

namespace UnityPlayerLogAnalyzer
{
    public class SourceFile
    {
        readonly string[] m_Lines;

        public SourceFile( string sourceText )
        {
            List<string> temp = new List<string>( );
            temp.Add( "" ); // Offset first [0] line to have first line start at 1
            temp.AddRange( sourceText.Split( '\n' ) );

            m_Lines = temp.ToArray( );
        }

        public string this[int index] => m_Lines[index];
        
        public int Length => m_Lines.Length;

        /// <summary>
        /// The range from-to is inclusive max.
        /// </summary>
        public string CaptureTextFromToLine( int from, int to )
        {
            return string.Join( "\n", m_Lines, from, to - from + 1 );
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
        public int SearchForDoubleNewLine( int i, SearchDirection dir, int limit = -1 )
        {
            if( dir == SearchDirection.Up )
            {
                if( limit < 0 ) // Default limit for searching up
                    limit = 0;

                while( i-- > limit )
                {
                    if( string.IsNullOrWhiteSpace( m_Lines[i] ) ) // As all lines are already splitted, if there is empty line that's mean there is double newline there
                        return i + 1;
                }
            }
            else
            {
                if( limit < 0 ) // Default limit for searching down
                    limit = Length;

                while( ++i < limit )
                {
                    if( string.IsNullOrWhiteSpace( m_Lines[i] ) )
                        return i - 1;
                }
            }
            return i;
        }

        public int SearchForLineContaining( int i, string text, SearchDirection dir, int limit = -1 )
        {
            if( dir == SearchDirection.Up )
            {
                if( limit < 0 ) // Default limit for searching up
                    limit = 0;

                while( i-- > limit )
                {
                    if( m_Lines[i].Contains( text ) )
                        return i;
                }
            }
            else
            {
                if( limit < 0 ) // Default limit for searching down
                    limit = Length;

                while( ++i < limit )
                {
                    if( m_Lines[i].Contains( text ) )
                        return i;
                }
            }

            return -1; // Not found
        }

        public enum SearchDirection
        {
            Up, Down
        }
    }
}
