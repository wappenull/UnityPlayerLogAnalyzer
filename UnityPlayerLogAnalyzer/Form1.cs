﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static UnityPlayerLogAnalyzer.SourceFile;
using YamlDotNet;
using YamlDotNet.Serialization;

namespace UnityPlayerLogAnalyzer
{
    public partial class Form1 : Form
    {
        public Form1( )
        {
            var builder = new SerializerBuilder( );
            //builder.WithTypeInspector( inspector => new FieldTypeInspector( inspector ) );
            builder.DisableAliases( ); // Allows item of repeated name to be serialized
            //builder.WithEventEmitter(nextEmitter => new MultilineScalarFlowStyleEmitter(nextEmitter)); // Force all multiline into | mode
            ISerializer ss = builder.Build( );

            InitializeComponent( );
        }

        private void Form1_Load( object sender, EventArgs e )
        {
            // Inject product version to title bar
            this.Text = Text + $" V. {ProductVersion}";
            optGrouping.SelectedIndex = 0;
        }

        private void Form1_DragDrop( object sender, DragEventArgs e )
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach( string file in files )
            {
                _ConvertSafe( file );
                Debug.WriteLine( "file is here! " + file );
            }
        }

        bool m_IgnoreMessageLog;
        int m_GroupingStyle;
        List<LogLine> m_LogLines;

        private void _ConvertSafe( string path )
        {
            try
            {
                _ConvertImpl( path );
            }
            catch( Exception e )
            {
                string msg = "There is an error parsing the log file. Probably the code is not strong enough to parse...." +
                    "\nFork the code to fix this or report to this dumb dumb developer. The message is copied to your clipboard." +
                    "\nThe exception reported is:" +
                    "\n\n" + e.ToString( );

                Clipboard.SetText( msg );
                MessageBox.Show( msg, "Teehee!", MessageBoxButtons.OK, MessageBoxIcon.Error );
            }
        }

        private void _ConvertImpl( string path )
        {
            m_IgnoreMessageLog = optNoLog.Checked;
            m_GroupingStyle = optGrouping.SelectedIndex;

            // Use sb because it is faster and less garbage
            StringBuilder sb = new StringBuilder( System.IO.File.ReadAllText( path ) );

            // Sanitize it, in this order
            // \r\n to \n
            // \r to \n
            sb.Replace( "\r\n", "\n" );
            sb.Replace( "\r", "\n" );

            // Extract logging part
            // Strategy is to revolve around "UnityEngine.Debug:Log" on start of the line, simple and stupid, no regex
            m_LogLines = new List<LogLine>( );
            SourceFile source = new SourceFile( sb.ToString( ) );
            sb.Clear( );
            _ExtractLogLines( m_LogLines, source );

            if( m_LogLines.Count == 0 )
            {
                MessageBox.Show( $"Program cannot detect pattern such as\n{CaptureMethod1.LogKeyword}\nLog file might be from non-dev mode or script debugging turned off.\nNow giveup or go to github to modify this program yourself!\n\nTeehee!", "Teehee!", MessageBoxButtons.OK, MessageBoxIcon.Error );
                return;
            }
            
            _WriteToOutputLog( sb, source, m_LogLines );

            string outputPath = path + ".yaml";
            System.IO.File.WriteAllText( outputPath, sb.ToString( ) );
            label1.Text = $"Output to {outputPath}\nLog line found {m_LogLines.Count}";
            MessageBox.Show( "Done" );
        }

        private void _ExtractLogLines( List<LogLine> logs, SourceFile source )
        {
            int sequence = 1;
            for( int i=0 ; i<source.Length ; i++ )
            {
                // Pattern 1
                if( CaptureMethod1.Detect( source[i] ) )
                {
                    LogLine ll = new LogLine( );
                    ll.sequence = sequence;
                    sequence++;

                    CaptureMethod1.CaptureLogLineAround( i, source, ll );
                    _AddByGrouping( ll, logs, m_GroupingStyle );

                    i = ll.endAtSourceLine + 1;
                }
                else if( CaptureMethod2.Detect( source[i] ) )
                {
                    LogLine ll = new LogLine( );
                    ll.sequence = sequence;
                    sequence++;

                    CaptureMethod2.CaptureLogLineAround( i, source, ll );
                    _AddByGrouping( ll, logs, m_GroupingStyle );

                    i = ll.endAtSourceLine + 1;
                }
            }
                 
        }

        private static void _AddByGrouping( LogLine ll, List<LogLine> logs, int groupingStyle )
        {
            // Depends on grouping option, we could ignore this if it is identical source from last log
            if( groupingStyle == 0 )
            {
                // Always add
            }
            else if( groupingStyle == 1 )
            {
                // Only same as last message
                LogLine last = null;
                if( logs.Count > 0 )
                    last = logs[logs.Count-1];

                // Compare with last one
                if( last != null )
                {
                    if( last.SameWith( ll ) )
                    {
                        last.repeat++;
                        return; // Skip to next
                    }
                }
            }
            else if( groupingStyle == 2 ) // Group any
            {
                // Search all past logs
                for( int i=0 ; i<logs.Count ; i++ )
                {
                    if( logs[i].SameWith( ll ) )
                    {
                        logs[i].repeat++;
                        return;
                    }
                }
            }

            logs.Add( ll );
        }

        /* Outputtting //////////////////////////////////////////////*/

        private void _WriteToOutputLog( StringBuilder sb, SourceFile source, List<LogLine> logLines )
        {
            sb.AppendLine( );
            sb.AppendLine( $"# Exported with Wappen's UnityPlayerLogAnalyzer =====================" );
            sb.AppendLine( $"# Export options" );
            sb.AppendLine( $"Version: {ProductVersion}" );
            sb.AppendLine( $"ExcludeLog: {m_IgnoreMessageLog}" );
            sb.AppendLine( $"GroupingStyle: {m_GroupingStyle}" );
            sb.AppendLine( $"# ===================================================================" );
            sb.AppendLine( );
          
            // Count all types
            int log, warning, error;
            log = warning = error = 0;
            foreach( LogLine ll in logLines )
            {
                switch( ll.logType )
                {
                    case LogLine.LogType.Error: error++; break;
                    case LogLine.LogType.Warning: warning++; break;
                    default: log++; break;
                }
            }

            sb.AppendLine( $"# Counters" );
            sb.AppendLine( $"Error: {error}" );
            sb.AppendLine( $"Warning: {warning}" );
            string ignoreMsg = m_IgnoreMessageLog ? " # Ignored" : "";
            sb.AppendLine( $"Log: {log}{ignoreMsg}" );
            sb.AppendLine( );

            sb.AppendLine( "# Log lines start here! ----------------------------------------------" );
            sb.AppendLine( );

            _PrintClientMachineInfo( sb, source );

            foreach( LogLine ll in logLines )
                _WriteToOutputLogSingle( sb, ll );
        }

        private void _PrintClientMachineInfo( StringBuilder sb, SourceFile source )
        {
            // Output yaml here
            int firstLogStartsAtLine = source.Length;
            if( m_LogLines.Count > 0 )
                firstLogStartsAtLine = m_LogLines[0].startFromSourceLine;

            // Print first part because it captures client machine info and other etc.
            sb.AppendLine( source.CaptureTextFromToLine( 0, firstLogStartsAtLine - 1 ) );
        }

        private void _WriteToOutputLogSingle( StringBuilder sb, LogLine ll )
        {
            if( ll.logType == LogLine.LogType.Log && m_IgnoreMessageLog )
                return;

            // Header
            sb.Append( $"{ll.logType} {ll.sequence}: " );
            string msg = ll.message.Replace( "\n", "\n  " ); // If there is newline in message, indent it
            if( ll.logType == LogLine.LogType.Error )
                sb.Append( "|" ); // Start with yaml multiline lateral to hightlight it to another color
            else if( ll.logType == LogLine.LogType.Warning )
                sb.Append( "|" );
            else if( ll.logType == LogLine.LogType.Log )
                sb.Append( "#" );

            if( ll.repeat > 1 )
                sb.Append( $"(x{ll.repeat}) " ); // Show collapsed occurrence

            sb.AppendLine( msg );

            // Callstack
            const string StartListIndent = "  - ";
            string c = ll.callstack.Replace( "\n", "\n" + StartListIndent ); // Insert indent for each line
            sb.AppendLine( "  Callstack:" );
            sb.AppendLine( StartListIndent + c );

            sb.AppendLine( ); // One last blank line for entry to separated when expanded
        }

        private void Form1_DragEnter( object sender, DragEventArgs e )
        {
            // Display nice icon
            if( e.Data.GetDataPresent( DataFormats.FileDrop ) ) e.Effect = DragDropEffects.Copy;

        }

        private void WebLinkClicked( object sender, LinkLabelLinkClickedEventArgs e )
        {
            LinkLabel lb = (LinkLabel)sender;
            string url;
            if( e.Link.LinkData != null )
                url = e.Link.LinkData.ToString( );
            else
                url = lb.Text;

            if( !url.Contains( "://" ) )
                url = "https://" + url;

            var si = new ProcessStartInfo(url);
            si.UseShellExecute = true;
            Process.Start( si );
            lb.LinkVisited = true;
        }
    }
}
