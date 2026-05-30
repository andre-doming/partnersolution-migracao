using System;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Aon.Utilities.UserInterface {

	/// <summary>
	/// Classe que contem metodo de extensão para facilitar o trabalho com os user controls
	/// </summary>
	public static class UIExtMethods {

		#region recuperacao de dados
		
		public static int GetInt32( this ITextControl textControl ) {
			return Convert.ToInt32( textControl.Text );
		}

		public static int? GetInt32Null( this ITextControl textControl ) {
			if ( textControl.Text.Equals( string.Empty ) )
				return null;
			return Convert.ToInt32( textControl.Text );
		}

		public static Int16 GetShort( this ITextControl textControl ) {
			return Convert.ToInt16( textControl.Text );
		}

		public static Int16? GetShortNull( this ITextControl textControl ) {
			if ( textControl.Text.Equals( string.Empty ) )
				return null;
			return Convert.ToInt16( textControl.Text );
		}

		public static DateTime GetDateTime( this ITextControl textControl ) {
			return Convert.ToDateTime( textControl.Text );
		}

		public static DateTime? GetDateTimeNull( this ITextControl textControl ) {
			if (string.IsNullOrEmpty(textControl.Text))
				return null;
			return Convert.ToDateTime( textControl.Text );
		}

		public static decimal GetDecimal( this ITextControl textControl ) {
			return Convert.ToDecimal( textControl.Text );
		}

		public static decimal? GetDecimalNull( this ITextControl textControl ) {
			if ( textControl.Text.Equals( string.Empty ) )
				return null;
			return Convert.ToDecimal( textControl.Text );
		}

		#endregion

		#region atribuicao de valores

		public static void Enable( this WebControl textControl ) {
			textControl.Enabled = true; 
		}

		public static void Disable( this WebControl textControl ) {
			textControl.Enabled = false;
		}

		public static void Clear( this ITextControl textControl ) {
			textControl.Text = string.Empty;
		}

		public static void SetInt32( this ITextControl textControl, int value ) {
			textControl.Text = value.ToString();
		}

		public static void SetInt32Null( this ITextControl textControl, int? value ) {
			textControl.Text = string.Empty;
			if ( value.HasValue )
				textControl.Text = value.Value.ToString();
		}

		public static void SetDateTime( this ITextControl textControl, DateTime value ) {
			textControl.Text = value.ToString();
		}

		public static void SetDateTime( this ITextControl textControl, DateTime value, string format ) {
			textControl.Text = value.ToString( format );
		}

		public static void SetDateTimeNull( this ITextControl textControl, DateTime? value ) {
			textControl.Text = string.Empty;
			if ( value.HasValue )
				textControl.Text = value.Value.ToString();
		}

		public static void SetDateTimeNull( this ITextControl textControl, DateTime? value, string format ) {
			textControl.Text = string.Empty;
			if ( value.HasValue )
				textControl.Text = value.Value.ToString( format );
		}

		public static void SetDecimal( this ITextControl textControl, decimal value ) {
			textControl.Text = value.ToString();
		}

		public static void SetDecimal( this ITextControl textControl, decimal value, string format ) {
			textControl.Text = value.ToString( format );
		}

		public static void SetDecimal( this ITextControl textControl, decimal? value ) {
			textControl.Text = string.Empty;
			if ( value.HasValue )
				textControl.Text = value.Value.ToString();
		}

		public static void SetDecimal( this ITextControl textControl, decimal? value, string format ) {
			textControl.Text = string.Empty;
			if ( value.HasValue )
				textControl.Text = value.Value.ToString( format );
		}

		public static void Clear( this GridView gridview ) {
			gridview.DataSource = null;
			gridview.DataBind();			
		}

		public static void BindData( this DropDownList dropdownlist, object data ) {
			dropdownlist.DataSource = data;
			dropdownlist.DataBind();
		}

		public static void BindData( this DropDownList dropDownlist, object data, ListItem itemDefault ) {
			dropDownlist.DataSource = data;
			dropDownlist.DataBind();
			dropDownlist.Items.Insert( 0, itemDefault );
		}

		public static void BindData( this DropDownList dropDownlist, object data, bool addDefaultItem ) {
			dropDownlist.DataSource = data;
			dropDownlist.DataBind();
			dropDownlist.Items.Insert( 0, new ListItem( "", "0" ) );
		}
		

		#endregion
	}
}
