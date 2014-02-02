﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web.Mvc;
using System.Xml.Linq;
using MvcSiteMapProvider.Builder;
using MvcSiteMapProvider.Caching;
using MvcSiteMapProvider.DI;
using MvcSiteMapProvider.Web.Script.Serialization;

namespace MvcSiteMapProvider.Collections.Specialized
{
    /// <summary>
    /// Specialized dictionary for providing business logic that manages
    /// the behavior of the route values.
    /// </summary>
    [ExcludeFromAutoRegistration]
    public class RouteValueDictionary
        : CacheableDictionary<string, object>, IRouteValueDictionary
    {
        public RouteValueDictionary(
            string siteMapNodeKey,
            string memberName,
            ISiteMap siteMap,
            IReservedAttributeNameProvider reservedAttributeNameProvider,
            IJsonToDictionaryDeserializer jsonToDictionaryDeserializer,
            ICache cache
            ) : base(siteMap, cache)
        {
            if (string.IsNullOrEmpty(siteMapNodeKey))
                throw new ArgumentNullException("siteMapNodeKey");
            if (string.IsNullOrEmpty(memberName))
                throw new ArgumentNullException("memberName");
            if (reservedAttributeNameProvider == null)
                throw new ArgumentNullException("reservedAttributeNameProvider");
            if (jsonToDictionaryDeserializer == null)
                throw new ArgumentNullException("jsonToDictionaryDeserializer");

            this.siteMapNodeKey = siteMapNodeKey;
            this.memberName = memberName;
            this.reservedAttributeNameProvider = reservedAttributeNameProvider;
            this.jsonToDictionaryDeserializer = jsonToDictionaryDeserializer;

            // An area route value must always exist, so we add it here to ensure it does.
            this["area"] = string.Empty;
        }

        protected readonly string siteMapNodeKey;
        protected readonly string memberName;
        protected readonly IReservedAttributeNameProvider reservedAttributeNameProvider;
        protected readonly IJsonToDictionaryDeserializer jsonToDictionaryDeserializer;

        protected override string GetCacheKey()
        {
            return "__ROUTE_VALUE_DICTIONARY_" + this.siteMap.CacheKey + "_" + this.siteMapNodeKey + "_" + this.memberName + "_";
        }

        /// <summary>
        /// Adds a new element to the dictionary with the specified key and value. If the key exists, the value will be overwritten.
        /// </summary>
        /// <param name="item">The KeyValuePair object that contains the key and value to add.</param>
        public override void Add(string key, object value)
        {
            this.Add(key, value, true);
        }

        /// <summary>
        /// Adds a new element to the dictionary with the values specified in the KeyValuePair. If the key exists, the value will be overwritten.
        /// </summary>
        /// <param name="item">The KeyValuePair object that contains the key and value to add.</param>
        public override void Add(KeyValuePair<string, object> item)
        {
            this.Add(item.Key, item.Value, true);
        }

        /// <summary>
        /// Adds a new element to the dictionary with the values specified in the KeyValuePair. If the key exists, the value will be overwritten.
        /// </summary>
        /// <param name="item">The KeyValuePair object that contains the key and value to add.</param>
        /// <param name="throwIfReservedKey"><c>true</c> to throw an exception if one of the keys being added is a reserved key name; otherwise, <c>false</c>.</param>
        public void Add(KeyValuePair<string, object> item, bool throwIfReservedKey)
        {
            this.Add(item.Key, item.Value, throwIfReservedKey);
        }

        /// <summary>
        /// Adds a new element to the dictionary with the specified key and value. If the key exists, the value will be overwritten.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add. The value can be null for reference types.</param>
        /// <param name="throwIfReservedKey"><c>true</c> to throw an exception if one of the keys being added is a reserved key name; otherwise, <c>false</c>.</param>
        public void Add(string key, object value, bool throwIfReservedKey)
        {
            if (this.reservedAttributeNameProvider.IsRouteAttribute(key))
            {
                if (!this.ContainsKey(key))
                    base.Add(key, value);
                else
                    base[key] = value;
            }
            else if (throwIfReservedKey)
            {
                throw new ReservedKeyException(string.Format(Resources.Messages.RouteValueKeyReserved, this.siteMapNodeKey, key, value));
            }
        }

        /// <summary>
        /// Adds the elements from a <see cref="System.Collections.Generic.IDictionary{string, object}"/>. If the key exists, the value will be overwritten.
        /// </summary>
        /// <param name="items">The <see cref="System.Collections.Generic.IDictionary{string, object}"/> of items to add.</param>
        public override void AddRange(IDictionary<string, object> items)
        {
            this.AddRange(items, true);
        }

        /// <summary>
        /// Adds the elements from a <see cref="System.Collections.Generic.IDictionary{string, object}"/>. If the key exists, the value will be overwritten.
        /// </summary>
        /// <param name="items">The <see cref="System.Collections.Generic.IDictionary{string, object}"/> of items to add.</param>
        /// <param name="throwIfReservedKey"><c>true</c> to throw an exception if one of the keys being added is a reserved key name; otherwise, <c>false</c>.</param>
        public void AddRange(IDictionary<string, object> items, bool throwIfReservedKey)
        {
            foreach (var item in items)
            {
                this.Add(item.Key, item.Value, throwIfReservedKey);
            }
        }

        /// <summary>
        /// Adds the elements from a JSON string representing the attributes. If the key exists, the value will be overwritten.
        /// </summary>
        /// <param name="jsonString">A JSON string that represents a dictionary of key-value pairs. Example: @"{ ""key-1"": ""value-1""[, ""key-x"": ""value-x""] }". 
        /// The value may be a string or primitive type (by leaving off the quotes).</param>
        public void AddRange(string jsonString)
        {
            this.AddRange(jsonString, true);
        }

        /// <summary>
        /// Adds the elements from a JSON string representing the attributes. If the key exists, the value will be overwritten.
        /// </summary>
        /// <param name="jsonString">A JSON string that represents a dictionary of key-value pairs. Example: @"{ ""key-1"": ""value-1""[, ""key-x"": ""value-x""] }". 
        /// The value may be a string or primitive type (by leaving off the quotes).</param>
        /// <param name="throwIfReservedKey"><c>true</c> to throw an exception if one of the keys being added is a reserved key name; otherwise, <c>false</c>.</param>
        public void AddRange(string jsonString, bool throwIfReservedKey)
        {
            var items = this.jsonToDictionaryDeserializer.Deserialize(jsonString);
            this.AddRange(items);
        }

        /// <summary>
        /// Adds the elements from a given <see cref="System.Xml.Linq.XElement"/>. If the key exists, the value will be overwritten.
        /// </summary>
        /// <param name="xmlNode">The <see cref="System.Xml.Linq.XElement"/> that represents the siteMapNode element in XML.</param>
        public void AddRange(XElement xmlNode)
        {
            this.AddRange(xmlNode, true);
        }

        /// <summary>
        /// Adds the elements from a given <see cref="System.Xml.Linq.XElement"/>. If the key exists, the value will be overwritten.
        /// </summary>
        /// <param name="xmlNode">The <see cref="System.Xml.Linq.XElement"/> that represents the siteMapNode element in XML.</param>
        /// <param name="throwIfReservedKey"><c>true</c> to throw an exception if one of the keys being added is a reserved key name; otherwise, <c>false</c>.</param>
        public void AddRange(XElement xmlNode, bool throwIfReservedKey)
        {
            foreach (XAttribute attribute in xmlNode.Attributes())
            {
                this.Add(attribute.Name.ToString(), attribute.Value, throwIfReservedKey);
            }
        }

        /// <summary>
        /// Adds the elements from a given <see cref="System.Collections.Specialized.NameValueCollection"/>. If the key exists, the value will be overwritten.
        /// </summary>
        /// <param name="nameValueCollection">The <see cref="System.Collections.Specialized.NameValueCollection"/> to retrieve the values from.</param>
        public void AddRange(NameValueCollection nameValueCollection)
        {
            this.AddRange(nameValueCollection, true);
        }

        /// <summary>
        /// Adds the elements from a given <see cref="System.Collections.Specialized.NameValueCollection"/>. If the key exists, the value will be overwritten.
        /// </summary>
        /// <param name="nameValueCollection">The <see cref="System.Collections.Specialized.NameValueCollection"/> to retrieve the values from.</param>
        /// <param name="throwIfReservedKey"><c>true</c> to throw an exception if one of the keys being added is a reserved key name; otherwise, <c>false</c>.</param>
        public void AddRange(NameValueCollection nameValueCollection, bool throwIfReservedKey)
        {
            foreach (string key in nameValueCollection.Keys)
            {
                this.Add(key, nameValueCollection[key], throwIfReservedKey);
            }
        }

        protected override void Insert(string key, object value, bool add)
        {
            this.Insert(key, value, add, true);
        }

        protected void Insert(string key, object value, bool add, bool throwIfReservedKey)
        {
            if (this.reservedAttributeNameProvider.IsRouteAttribute(key))
            {
                base.Insert(key, value, add);
            }
            else if (throwIfReservedKey)
            {
                throw new ReservedKeyException(string.Format(Resources.Messages.RouteValueKeyReserved, this.siteMapNodeKey, key, value));
            }
        }

        public virtual bool MatchesRoute(IEnumerable<string> actionParameters, IDictionary<string, object> routeValues)
        {
            if (routeValues.Count > 0)
            {
                foreach (var pair in routeValues)
                {
                    if (!this.MatchesRouteValue(actionParameters, pair.Key, pair.Value))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        protected virtual bool MatchesRouteValue(IEnumerable<string> actionParameters, string key, object value)
        {
            if (this.ValueExists(key))
            {
                if (this.MatchesValue(key, value) || this.MatchesActionMethodParameter(actionParameters, key))
                {
                    return true;
                }
            }
            else
            {
                if (this.IsEmptyValue(value))
                {
                    return true;
                }
            }
            return false;
        }

        protected virtual bool MatchesActionMethodParameter(IEnumerable<string> actionParameters, string key)
        {
            return actionParameters.Contains(key, StringComparer.InvariantCultureIgnoreCase);
        }

        protected virtual bool MatchesValue(string key, object value)
        {
            return this[key].ToString().Equals(value.ToString(), StringComparison.InvariantCultureIgnoreCase);
        }

        protected virtual bool IsEmptyValue(object value)
        {
            return value == null ||
                string.IsNullOrEmpty(value.ToString()) || 
                value == UrlParameter.Optional;
        }

        public override object this[string key]
        {
            get
            {
                return base[key];
            }
            set
            {
                if (this.reservedAttributeNameProvider.IsRouteAttribute(key))
                {
                    base[key] = value;
                }
                else
                {
                    throw new ReservedKeyException(string.Format(Resources.Messages.RouteValueKeyReserved, this.siteMapNodeKey, key, value));
                }
            }
        }

        protected virtual bool ValueExists(string key)
        {
            return this.ContainsKey(key) && 
                this[key] != null && 
                !string.IsNullOrEmpty(this[key].ToString());
        }
    }
}
