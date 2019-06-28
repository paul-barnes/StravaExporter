/* 
 * Strava API v3
 *
 * Strava API
 *
 * OpenAPI spec version: 3.0.0
 * 
 * Generated by: https://github.com/swagger-api/swagger-codegen.git
 */

using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;
using SwaggerDateConverter = IO.Swagger.Client.SwaggerDateConverter;

namespace IO.Swagger.Model
{
    /// <summary>
    /// PolylineMap
    /// </summary>
    [DataContract]
    public partial class PolylineMap :  IEquatable<PolylineMap>, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PolylineMap" /> class.
        /// </summary>
        /// <param name="id">The identifier of the map.</param>
        /// <param name="polyline">The polyline of the map.</param>
        /// <param name="summaryPolyline">The summary polyline of the map.</param>
        public PolylineMap(string id = default(string), string polyline = default(string), string summaryPolyline = default(string))
        {
            this.Id = id;
            this.Polyline = polyline;
            this.SummaryPolyline = summaryPolyline;
        }
        
        /// <summary>
        /// The identifier of the map
        /// </summary>
        /// <value>The identifier of the map</value>
        [DataMember(Name="id", EmitDefaultValue=false)]
        public string Id { get; set; }

        /// <summary>
        /// The polyline of the map
        /// </summary>
        /// <value>The polyline of the map</value>
        [DataMember(Name="polyline", EmitDefaultValue=false)]
        public string Polyline { get; set; }

        /// <summary>
        /// The summary polyline of the map
        /// </summary>
        /// <value>The summary polyline of the map</value>
        [DataMember(Name="summary_polyline", EmitDefaultValue=false)]
        public string SummaryPolyline { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class PolylineMap {\n");
            sb.Append("  Id: ").Append(Id).Append("\n");
            sb.Append("  Polyline: ").Append(Polyline).Append("\n");
            sb.Append("  SummaryPolyline: ").Append(SummaryPolyline).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }
  
        /// <summary>
        /// Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public virtual string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        /// Returns true if objects are equal
        /// </summary>
        /// <param name="input">Object to be compared</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object input)
        {
            return this.Equals(input as PolylineMap);
        }

        /// <summary>
        /// Returns true if PolylineMap instances are equal
        /// </summary>
        /// <param name="input">Instance of PolylineMap to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(PolylineMap input)
        {
            if (input == null)
                return false;

            return 
                (
                    this.Id == input.Id ||
                    (this.Id != null &&
                    this.Id.Equals(input.Id))
                ) && 
                (
                    this.Polyline == input.Polyline ||
                    (this.Polyline != null &&
                    this.Polyline.Equals(input.Polyline))
                ) && 
                (
                    this.SummaryPolyline == input.SummaryPolyline ||
                    (this.SummaryPolyline != null &&
                    this.SummaryPolyline.Equals(input.SummaryPolyline))
                );
        }

        /// <summary>
        /// Gets the hash code
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hashCode = 41;
                if (this.Id != null)
                    hashCode = hashCode * 59 + this.Id.GetHashCode();
                if (this.Polyline != null)
                    hashCode = hashCode * 59 + this.Polyline.GetHashCode();
                if (this.SummaryPolyline != null)
                    hashCode = hashCode * 59 + this.SummaryPolyline.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// To validate all properties of the instance
        /// </summary>
        /// <param name="validationContext">Validation context</param>
        /// <returns>Validation Result</returns>
        IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            yield break;
        }
    }

}