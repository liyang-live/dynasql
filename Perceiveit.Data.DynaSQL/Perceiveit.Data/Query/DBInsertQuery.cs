/*  Copyright 2009 PerceiveIT Limited
 *  This file is part of the DynaSQL library.
 *
*  DynaSQL is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  DynaSQL is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with Query in the COPYING.txt file.  If not, see <http://www.gnu.org/licenses/>.
 * 
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace Perceiveit.Data.Query
{
    public class DBInsertQuery : DBQuery
    {

        private DBTable _into;
        private DBSelectSet _fields;
        private DBValueSet _values;
        private DBSelectQuery _innerselect;
        private DBClause _last;


        //
        // properties
        //

        #region internal DBTableSet IntoSet {get;set;}

        internal DBTable Into
        {
            get { return this._into; }
            set { this._into = value; }
        }

        #endregion

        #region internal DBSelectSet FieldsSet {get;set;}

        internal DBSelectSet FieldsSet
        {
            get { return this._fields; }
            set { this._fields = value; }
        }

        #endregion

        #region internal DBValueSet ValueSet {get;set;}

        internal DBValueSet ValueSet
        {
            get { return this._values; }
            set { this._values = value; }
        }

        #endregion

        #region internal DBSelectQuery InnerSelect {get;set;}

        internal DBSelectQuery InnerSelect
        {
            get { return this._innerselect; }
            set { this._innerselect = value; }
        }

        #endregion

        #region protected internal DBClause Last {get;set;}

        protected internal DBClause Last
        {
            get { return this._last; }
            set { this._last = value; }
        }

        #endregion

        //
        // public methods
        //

        #region public DBInsertQuery Field(string field) + 4 overloads

        public DBInsertQuery Field(string owner, string table, string field)
        {
            DBField fld = DBField.Field(owner, table, field);
            return Field(fld);
        }

        public DBInsertQuery Field(string table, string field)
        {
            DBField fld = DBField.Field(table, field);
            return Field(fld);
        }

        public DBInsertQuery Field(string field)
        {
            DBField fld = DBField.Field(field);
            return Field(fld);
        }
        
        public DBInsertQuery Field(DBClause field)
        {
            if (_fields == null)
                _last = _fields = DBSelectSet.Select(field);
            else
                _last = _fields.And(field);
            return this;
        }

        public DBInsertQuery Fields(params string[] fields)
        {
            if (_fields == null)
                _last = _fields = DBSelectSet.SelectFields(fields);
            else
            {
                foreach (string fld in fields)
                {
                    _last = _fields.And(fld);

                }
            }
            return this;
        }

        #endregion


        #region public DBInsertQuery Value(ParamValue valueProvider) + 2 overloads

        public DBInsertQuery Value(ParamValue valueProvider)
        {
            DBParam p = DBParam.ParamWithDelegate(valueProvider);
            return Value(p);
        }

        public DBInsertQuery Value(DBClause clause)
        {
            if (_innerselect != null)
                throw new ArgumentException("Cannot assign from values if the insert statement has an existing select statement");

            if (_values == null)
                _values = DBValueSet.Values();
            _last = _values.And(clause);
            return this;
        }

        public DBInsertQuery Values(params DBClause[] values)
        {
            if (_innerselect != null)
                throw new ArgumentException("Cannot assign from values if the insert statement has an existing select statement");

            if (_values == null)
                _values = DBValueSet.Values();

            foreach (DBClause val in values)
            {
                _last = _values.And(val);

            }

            return this;
        }

        #endregion

        #region public DBInsertQuery Select(DBSelectQuery select)

        public DBInsertQuery Select(DBSelectQuery select)
        {
            if(_values != null)
                throw new ArgumentException("Cannot assign from values if the insert statement has an existing select statement");

            _innerselect = select;
            _innerselect.IsInnerQuery = true;
            _last = select;
            return this;
        }

        #endregion

        //
        // build method(s)
        //

        #region public override bool BuildStatement(DBStatementBuilder builder)

        public override bool BuildStatement(DBStatementBuilder builder)
        {
            builder.BeginInsertStatement();
            if (_into == null)
                throw new NullReferenceException("No table defined to insert into");

            _into.BuildStatement(builder);
            if (this._fields != null && this._fields.Results.Count > 0)
            {
                builder.BeginInsertFieldList();
                _fields.BuildStatement(builder);
                builder.EndInsertFieldList();
            }
            if (_values != null && _values.HasClauses)
            {
                builder.BeginInsertValueList();
                _values.BuildStatement(builder);
                builder.EndInsertValueList();
            }
            else if(_innerselect != null)
            {
                builder.BeginSubStatement();
                _innerselect.BuildStatement(builder);
                builder.EndSubStatement();
            }
            builder.EndInsertStatement();

            return true;

        }

        #endregion

        //
        // xml serialization methods
        //

        #region protected override string XmlElementName

        /// <summary>
        /// Overrides the default implmentation to return the name of the Insert XML element name
        /// </summary>
        protected override string XmlElementName
        {
            get
            {
                return XmlHelper.Insert;
            }
        }

        #endregion

        
        #region protected override bool WriteInnerElements(System.Xml.XmlWriter writer, XmlWriterContext context)
        
        protected override bool WriteInnerElements(System.Xml.XmlWriter writer, XmlWriterContext context)
        {
            if (this._into != null)
            {
                this.WriteStartElement(XmlHelper.Into, writer, context);
                this._into.WriteXml(writer, context);
                this.WriteEndElement(XmlHelper.Into, writer, context);
            }

            if (this._fields != null && this._fields.Results.Count > 0)
            {
                _fields.WriteXml(writer, context);
            }
            
            if (_values != null)
            {
                _values.WriteXml(writer, context);
            }
            else if(_innerselect != null)
            {
                _innerselect.WriteXml(writer, context);
            }

            return base.WriteInnerElements(writer, context);
        }

        #endregion

        #region protected override bool ReadAnInnerElement(System.Xml.XmlReader reader, XmlReaderContext context)
        

        protected override bool ReadAnInnerElement(System.Xml.XmlReader reader, XmlReaderContext context)
        {
            bool b;
            if (this.IsElementMatch(XmlHelper.Into, reader, context) && !reader.IsEmptyElement && reader.Read())
            {
                this._into = (DBTable)this.ReadNextInnerClause(XmlHelper.Into, reader, context);
                b = true;
            }
            else if (this.IsElementMatch(XmlHelper.Fields, reader, context))
            {
                this._fields = (DBSelectSet)context.Factory.Read(XmlHelper.Fields, reader, context);
                b = true;
            }
            else if (this.IsElementMatch(XmlHelper.Values, reader, context))
            {
                this._values = (DBValueSet)context.Factory.Read(XmlHelper.Values, reader, context);
                b = true;
            }
            else if (this.IsElementMatch(XmlHelper.Select, reader, context))
            {
                this._innerselect = (DBSelectQuery)context.Factory.Read(XmlHelper.Select, reader, context);
                this._innerselect.IsInnerQuery = true;
                b = true;
            }
            else 
                b = base.ReadAnInnerElement(reader, context);

            return b;
        }

        #endregion

    }
}
