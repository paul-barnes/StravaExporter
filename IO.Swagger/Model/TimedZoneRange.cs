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
    /// A union type representing the time spent in a given zone.
    /// </summary>
    [DataContract]
    public partial class TimedZoneRange :  IEquatable<TimedZoneRange>, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TimedZoneRange" /> class.
        /// </summary>
        /// <param name="min">The minimum value in the range..</param>
        /// <param name="max">The maximum value in the range..</param>
        /// <param name="time">The number of seconds spent in this zone.</param>
        public TimedZoneRange(int? min = default(int?), int? max = default(int?), int? time = default(int?))
        {
            this.Min = min;
            this.Max = max;
            this.Time = time;
        }
        
        /// <summary>
        /// The minimum value in the range.
        /// </summary>
        /// <value>The minimum value in the range.</value>
        [DataMember(Name="min", EmitDefaultValue=false)]
        public int? Min { get; set; }

        /// <summary>
        /// The maximum value in the range.
        /// </summary>
        /// <value>The maximum value in the range.</value>
        [DataMember(Name="max", EmitDefaultValue=false)]
        public int? Max { get; set; }

        /// <summary>
        /// The number of seconds spent in this zone
        /// </summary>
        /// <value>The number of seconds spent in this zone</value>
        [DataMember(Name="time", EmitDefaultValue=false)]
        public int? Time { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class TimedZoneRange {\n");
            sb.Append("  Min: ").Append(Min).Append("\n");
            sb.Append("  Max: ").Append(Max).Append("\n");
            sb.Append("  Time: ").Append(Time).Append("\n");
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
            return this.Equals(input as TimedZoneRange);
        }

        /// <summary>
        /// Returns true if TimedZoneRange instances are equal
        /// </summary>
        /// <param name="input">Instance of TimedZoneRange to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(TimedZoneRange input)
        {
            if (input == null)
                return false;

            return 
                (
                    this.Min == input.Min ||
                    (this.Min != null &&
                    this.Min.Equals(input.Min))
                ) && 
                (
                    this.Max == input.Max ||
                    (this.Max != null &&
                    this.Max.Equals(input.Max))
                ) && 
                (
                    this.Time == input.Time ||
                    (this.Time != null &&
                    this.Time.Equals(input.Time))
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
                if (this.Min != null)
                    hashCode = hashCode * 59 + this.Min.GetHashCode();
                if (this.Max != null)
                    hashCode = hashCode * 59 + this.Max.GetHashCode();
                if (this.Time != null)
                    hashCode = hashCode * 59 + this.Time.GetHashCode();
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
