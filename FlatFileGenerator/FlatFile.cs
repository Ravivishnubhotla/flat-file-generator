﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace FlatFile {
	/// <summary>
	/// Outputs a flat file of type T.
	/// </summary>
	/// <typeparam name="T">The type of the file to write, with properties that implement <see cref="FlatFileAttribute"/>.</typeparam>
	public sealed class FlatFile<T> where T : IFlatFile {
		public void WriteFile( List<T> records, string filePath ) {
			// Make sure there are records to write
			if( records.Count == 0 )
				return;

			using( var writer = File.CreateText( filePath ) ) {
				foreach( var line in records.Select( createLine ) ) {
					// write to file
					writer.WriteLine( line );
				}
			}
		}

		public void WriteFile( List<T> records, Stream stream ) {
			// Make sure there are records to write
			if( records.Count == 0 )
				return;

			foreach( var lineBytes in records.Select( createLine ).Select( stringToBytes ) ) {
				// write to file
				stream.Write( lineBytes, 0, lineBytes.Length );
			}
		}

		private static byte[ ] stringToBytes( string text ) {
			var bytes = new ASCIIEncoding( ).GetBytes( text + Environment.NewLine );
			return bytes;
		}

		private static string createLine( T record ) {
			var dataRow = String.Empty;
			var pi = record.GetType( ).GetProperties( );

			foreach( var p in pi ) {
				var attributes = p.GetCustomAttributes( typeof( FlatFileAttribute ), false );
				FlatFileAttribute ffa = null;

				if( attributes.Length > 0 ) {
					ffa = attributes[ 0 ] as FlatFileAttribute;
				}

				if( ffa == null )
					// skip properties without the FlatFileAttribute (e.g., IFlatFile.FixedLineWidth)
					continue;

				var start = ffa.StartPosition;
				var length = ffa.FieldLength;

				var outputString = Convert.ToString( p.GetValue( record, null ) );
				if( outputString.Length < length ) {
					outputString = outputString.PadRight( length );
				} else if( outputString.Length > length ) {
					outputString = outputString.Substring( 0, length );
				}

				if( dataRow.Length + 1 == start ) {
					dataRow += outputString;
				} else if( dataRow.Length < start ) {
					// need to extend the line out
					dataRow = dataRow.PadRight( start - 1 ) + outputString;
				} else {
					// current field falls inside the middle of the existing output string.
					// split based on start position and then concatenate
					var leftSide = dataRow.Substring( 0, start - 1 );
					var rightSide = dataRow.Substring( start, dataRow.Length );
					dataRow = leftSide + outputString + rightSide;
				}
			}

			if( record.FixedLineWidth > 0 ) {
				// row has a fixed width, trim or extend to specified length
				if( dataRow.Length > record.FixedLineWidth )
					dataRow = dataRow.Substring( 0, record.FixedLineWidth );
				else if( dataRow.Length < record.FixedLineWidth )
					dataRow = dataRow.PadRight( record.FixedLineWidth );
			}

			return dataRow;
		}
	}
}