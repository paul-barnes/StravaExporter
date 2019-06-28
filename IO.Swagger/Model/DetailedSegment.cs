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
    /// DetailedSegment
    /// </summary>
    [DataContract]
    public partial class DetailedSegment :  IEquatable<DetailedSegment>, IValidatableObject
    {
        /// <summary>
        /// Defines ActivityType
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum ActivityTypeEnum
        {
            
            /// <summary>
            /// Enum Ride for value: Ride
            /// </summary>
            [EnumMember(Value = "Ride")]
            Ride = 1,
            
            /// <summary>
            /// Enum Run for value: Run
            /// </summary>
            [EnumMember(Value = "Run")]
            Run = 2
        }

        /// <summary>
        /// Gets or Sets ActivityType
        /// </summary>
        [DataMember(Name="activity_type", EmitDefaultValue=false)]
        public ActivityTypeEnum? ActivityType { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="DetailedSegment" /> class.
        /// </summary>
        /// <param name="id">The unique identifier of this segment.</param>
        /// <param name="name">The name of this segment.</param>
        /// <param name="activityType">activityType.</param>
        /// <param name="distance">The segment&#39;s distance, in meters.</param>
        /// <param name="averageGrade">The segment&#39;s average grade, in percents.</param>
        /// <param name="maximumGrade">The segments&#39;s maximum grade, in percents.</param>
        /// <param name="elevationHigh">The segments&#39;s highest elevation, in meters.</param>
        /// <param name="elevationLow">The segments&#39;s lowest elevation, in meters.</param>
        /// <param name="startLatlng">startLatlng.</param>
        /// <param name="endLatlng">endLatlng.</param>
        /// <param name="climbCategory">The category of the climb [0, 5]. Higher is harder ie. 5 is Hors catégorie, 0 is uncategorized in climb_category..</param>
        /// <param name="city">The segments&#39;s city..</param>
        /// <param name="state">The segments&#39;s state or geographical region..</param>
        /// <param name="country">The segment&#39;s country..</param>
        /// <param name="_private">Whether this segment is private..</param>
        /// <param name="athletePrEffort">athletePrEffort.</param>
        /// <param name="createdAt">The time at which the segment was created..</param>
        /// <param name="updatedAt">The time at which the segment was last updated..</param>
        /// <param name="totalElevationGain">The segment&#39;s total elevation gain..</param>
        /// <param name="map">map.</param>
        /// <param name="effortCount">The total number of efforts for this segment.</param>
        /// <param name="athleteCount">The number of unique athletes who have an effort for this segment.</param>
        /// <param name="hazardous">Whether this segment is considered hazardous.</param>
        /// <param name="starCount">The number of stars for this segment.</param>
        public DetailedSegment(long? id = default(long?), string name = default(string), ActivityTypeEnum? activityType = default(ActivityTypeEnum?), float? distance = default(float?), float? averageGrade = default(float?), float? maximumGrade = default(float?), float? elevationHigh = default(float?), float? elevationLow = default(float?), LatLng startLatlng = default(LatLng), LatLng endLatlng = default(LatLng), int? climbCategory = default(int?), string city = default(string), string state = default(string), string country = default(string), bool? _private = default(bool?), SummarySegmentEffort athletePrEffort = default(SummarySegmentEffort), DateTime? createdAt = default(DateTime?), DateTime? updatedAt = default(DateTime?), float? totalElevationGain = default(float?), PolylineMap map = default(PolylineMap), int? effortCount = default(int?), int? athleteCount = default(int?), bool? hazardous = default(bool?), int? starCount = default(int?))
        {
            this.Id = id;
            this.Name = name;
            this.ActivityType = activityType;
            this.Distance = distance;
            this.AverageGrade = averageGrade;
            this.MaximumGrade = maximumGrade;
            this.ElevationHigh = elevationHigh;
            this.ElevationLow = elevationLow;
            this.StartLatlng = startLatlng;
            this.EndLatlng = endLatlng;
            this.ClimbCategory = climbCategory;
            this.City = city;
            this.State = state;
            this.Country = country;
            this.Private = _private;
            this.AthletePrEffort = athletePrEffort;
            this.CreatedAt = createdAt;
            this.UpdatedAt = updatedAt;
            this.TotalElevationGain = totalElevationGain;
            this.Map = map;
            this.EffortCount = effortCount;
            this.AthleteCount = athleteCount;
            this.Hazardous = hazardous;
            this.StarCount = starCount;
        }
        
        /// <summary>
        /// The unique identifier of this segment
        /// </summary>
        /// <value>The unique identifier of this segment</value>
        [DataMember(Name="id", EmitDefaultValue=false)]
        public long? Id { get; set; }

        /// <summary>
        /// The name of this segment
        /// </summary>
        /// <value>The name of this segment</value>
        [DataMember(Name="name", EmitDefaultValue=false)]
        public string Name { get; set; }


        /// <summary>
        /// The segment&#39;s distance, in meters
        /// </summary>
        /// <value>The segment&#39;s distance, in meters</value>
        [DataMember(Name="distance", EmitDefaultValue=false)]
        public float? Distance { get; set; }

        /// <summary>
        /// The segment&#39;s average grade, in percents
        /// </summary>
        /// <value>The segment&#39;s average grade, in percents</value>
        [DataMember(Name="average_grade", EmitDefaultValue=false)]
        public float? AverageGrade { get; set; }

        /// <summary>
        /// The segments&#39;s maximum grade, in percents
        /// </summary>
        /// <value>The segments&#39;s maximum grade, in percents</value>
        [DataMember(Name="maximum_grade", EmitDefaultValue=false)]
        public float? MaximumGrade { get; set; }

        /// <summary>
        /// The segments&#39;s highest elevation, in meters
        /// </summary>
        /// <value>The segments&#39;s highest elevation, in meters</value>
        [DataMember(Name="elevation_high", EmitDefaultValue=false)]
        public float? ElevationHigh { get; set; }

        /// <summary>
        /// The segments&#39;s lowest elevation, in meters
        /// </summary>
        /// <value>The segments&#39;s lowest elevation, in meters</value>
        [DataMember(Name="elevation_low", EmitDefaultValue=false)]
        public float? ElevationLow { get; set; }

        /// <summary>
        /// Gets or Sets StartLatlng
        /// </summary>
        [DataMember(Name="start_latlng", EmitDefaultValue=false)]
        public LatLng StartLatlng { get; set; }

        /// <summary>
        /// Gets or Sets EndLatlng
        /// </summary>
        [DataMember(Name="end_latlng", EmitDefaultValue=false)]
        public LatLng EndLatlng { get; set; }

        /// <summary>
        /// The category of the climb [0, 5]. Higher is harder ie. 5 is Hors catégorie, 0 is uncategorized in climb_category.
        /// </summary>
        /// <value>The category of the climb [0, 5]. Higher is harder ie. 5 is Hors catégorie, 0 is uncategorized in climb_category.</value>
        [DataMember(Name="climb_category", EmitDefaultValue=false)]
        public int? ClimbCategory { get; set; }

        /// <summary>
        /// The segments&#39;s city.
        /// </summary>
        /// <value>The segments&#39;s city.</value>
        [DataMember(Name="city", EmitDefaultValue=false)]
        public string City { get; set; }

        /// <summary>
        /// The segments&#39;s state or geographical region.
        /// </summary>
        /// <value>The segments&#39;s state or geographical region.</value>
        [DataMember(Name="state", EmitDefaultValue=false)]
        public string State { get; set; }

        /// <summary>
        /// The segment&#39;s country.
        /// </summary>
        /// <value>The segment&#39;s country.</value>
        [DataMember(Name="country", EmitDefaultValue=false)]
        public string Country { get; set; }

        /// <summary>
        /// Whether this segment is private.
        /// </summary>
        /// <value>Whether this segment is private.</value>
        [DataMember(Name="private", EmitDefaultValue=false)]
        public bool? Private { get; set; }

        /// <summary>
        /// Gets or Sets AthletePrEffort
        /// </summary>
        [DataMember(Name="athlete_pr_effort", EmitDefaultValue=false)]
        public SummarySegmentEffort AthletePrEffort { get; set; }

        /// <summary>
        /// The time at which the segment was created.
        /// </summary>
        /// <value>The time at which the segment was created.</value>
        [DataMember(Name="created_at", EmitDefaultValue=false)]
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// The time at which the segment was last updated.
        /// </summary>
        /// <value>The time at which the segment was last updated.</value>
        [DataMember(Name="updated_at", EmitDefaultValue=false)]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// The segment&#39;s total elevation gain.
        /// </summary>
        /// <value>The segment&#39;s total elevation gain.</value>
        [DataMember(Name="total_elevation_gain", EmitDefaultValue=false)]
        public float? TotalElevationGain { get; set; }

        /// <summary>
        /// Gets or Sets Map
        /// </summary>
        [DataMember(Name="map", EmitDefaultValue=false)]
        public PolylineMap Map { get; set; }

        /// <summary>
        /// The total number of efforts for this segment
        /// </summary>
        /// <value>The total number of efforts for this segment</value>
        [DataMember(Name="effort_count", EmitDefaultValue=false)]
        public int? EffortCount { get; set; }

        /// <summary>
        /// The number of unique athletes who have an effort for this segment
        /// </summary>
        /// <value>The number of unique athletes who have an effort for this segment</value>
        [DataMember(Name="athlete_count", EmitDefaultValue=false)]
        public int? AthleteCount { get; set; }

        /// <summary>
        /// Whether this segment is considered hazardous
        /// </summary>
        /// <value>Whether this segment is considered hazardous</value>
        [DataMember(Name="hazardous", EmitDefaultValue=false)]
        public bool? Hazardous { get; set; }

        /// <summary>
        /// The number of stars for this segment
        /// </summary>
        /// <value>The number of stars for this segment</value>
        [DataMember(Name="star_count", EmitDefaultValue=false)]
        public int? StarCount { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class DetailedSegment {\n");
            sb.Append("  Id: ").Append(Id).Append("\n");
            sb.Append("  Name: ").Append(Name).Append("\n");
            sb.Append("  ActivityType: ").Append(ActivityType).Append("\n");
            sb.Append("  Distance: ").Append(Distance).Append("\n");
            sb.Append("  AverageGrade: ").Append(AverageGrade).Append("\n");
            sb.Append("  MaximumGrade: ").Append(MaximumGrade).Append("\n");
            sb.Append("  ElevationHigh: ").Append(ElevationHigh).Append("\n");
            sb.Append("  ElevationLow: ").Append(ElevationLow).Append("\n");
            sb.Append("  StartLatlng: ").Append(StartLatlng).Append("\n");
            sb.Append("  EndLatlng: ").Append(EndLatlng).Append("\n");
            sb.Append("  ClimbCategory: ").Append(ClimbCategory).Append("\n");
            sb.Append("  City: ").Append(City).Append("\n");
            sb.Append("  State: ").Append(State).Append("\n");
            sb.Append("  Country: ").Append(Country).Append("\n");
            sb.Append("  Private: ").Append(Private).Append("\n");
            sb.Append("  AthletePrEffort: ").Append(AthletePrEffort).Append("\n");
            sb.Append("  CreatedAt: ").Append(CreatedAt).Append("\n");
            sb.Append("  UpdatedAt: ").Append(UpdatedAt).Append("\n");
            sb.Append("  TotalElevationGain: ").Append(TotalElevationGain).Append("\n");
            sb.Append("  Map: ").Append(Map).Append("\n");
            sb.Append("  EffortCount: ").Append(EffortCount).Append("\n");
            sb.Append("  AthleteCount: ").Append(AthleteCount).Append("\n");
            sb.Append("  Hazardous: ").Append(Hazardous).Append("\n");
            sb.Append("  StarCount: ").Append(StarCount).Append("\n");
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
            return this.Equals(input as DetailedSegment);
        }

        /// <summary>
        /// Returns true if DetailedSegment instances are equal
        /// </summary>
        /// <param name="input">Instance of DetailedSegment to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(DetailedSegment input)
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
                    this.Name == input.Name ||
                    (this.Name != null &&
                    this.Name.Equals(input.Name))
                ) && 
                (
                    this.ActivityType == input.ActivityType ||
                    (this.ActivityType != null &&
                    this.ActivityType.Equals(input.ActivityType))
                ) && 
                (
                    this.Distance == input.Distance ||
                    (this.Distance != null &&
                    this.Distance.Equals(input.Distance))
                ) && 
                (
                    this.AverageGrade == input.AverageGrade ||
                    (this.AverageGrade != null &&
                    this.AverageGrade.Equals(input.AverageGrade))
                ) && 
                (
                    this.MaximumGrade == input.MaximumGrade ||
                    (this.MaximumGrade != null &&
                    this.MaximumGrade.Equals(input.MaximumGrade))
                ) && 
                (
                    this.ElevationHigh == input.ElevationHigh ||
                    (this.ElevationHigh != null &&
                    this.ElevationHigh.Equals(input.ElevationHigh))
                ) && 
                (
                    this.ElevationLow == input.ElevationLow ||
                    (this.ElevationLow != null &&
                    this.ElevationLow.Equals(input.ElevationLow))
                ) && 
                (
                    this.StartLatlng == input.StartLatlng ||
                    (this.StartLatlng != null &&
                    this.StartLatlng.Equals(input.StartLatlng))
                ) && 
                (
                    this.EndLatlng == input.EndLatlng ||
                    (this.EndLatlng != null &&
                    this.EndLatlng.Equals(input.EndLatlng))
                ) && 
                (
                    this.ClimbCategory == input.ClimbCategory ||
                    (this.ClimbCategory != null &&
                    this.ClimbCategory.Equals(input.ClimbCategory))
                ) && 
                (
                    this.City == input.City ||
                    (this.City != null &&
                    this.City.Equals(input.City))
                ) && 
                (
                    this.State == input.State ||
                    (this.State != null &&
                    this.State.Equals(input.State))
                ) && 
                (
                    this.Country == input.Country ||
                    (this.Country != null &&
                    this.Country.Equals(input.Country))
                ) && 
                (
                    this.Private == input.Private ||
                    (this.Private != null &&
                    this.Private.Equals(input.Private))
                ) && 
                (
                    this.AthletePrEffort == input.AthletePrEffort ||
                    (this.AthletePrEffort != null &&
                    this.AthletePrEffort.Equals(input.AthletePrEffort))
                ) && 
                (
                    this.CreatedAt == input.CreatedAt ||
                    (this.CreatedAt != null &&
                    this.CreatedAt.Equals(input.CreatedAt))
                ) && 
                (
                    this.UpdatedAt == input.UpdatedAt ||
                    (this.UpdatedAt != null &&
                    this.UpdatedAt.Equals(input.UpdatedAt))
                ) && 
                (
                    this.TotalElevationGain == input.TotalElevationGain ||
                    (this.TotalElevationGain != null &&
                    this.TotalElevationGain.Equals(input.TotalElevationGain))
                ) && 
                (
                    this.Map == input.Map ||
                    (this.Map != null &&
                    this.Map.Equals(input.Map))
                ) && 
                (
                    this.EffortCount == input.EffortCount ||
                    (this.EffortCount != null &&
                    this.EffortCount.Equals(input.EffortCount))
                ) && 
                (
                    this.AthleteCount == input.AthleteCount ||
                    (this.AthleteCount != null &&
                    this.AthleteCount.Equals(input.AthleteCount))
                ) && 
                (
                    this.Hazardous == input.Hazardous ||
                    (this.Hazardous != null &&
                    this.Hazardous.Equals(input.Hazardous))
                ) && 
                (
                    this.StarCount == input.StarCount ||
                    (this.StarCount != null &&
                    this.StarCount.Equals(input.StarCount))
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
                if (this.Name != null)
                    hashCode = hashCode * 59 + this.Name.GetHashCode();
                if (this.ActivityType != null)
                    hashCode = hashCode * 59 + this.ActivityType.GetHashCode();
                if (this.Distance != null)
                    hashCode = hashCode * 59 + this.Distance.GetHashCode();
                if (this.AverageGrade != null)
                    hashCode = hashCode * 59 + this.AverageGrade.GetHashCode();
                if (this.MaximumGrade != null)
                    hashCode = hashCode * 59 + this.MaximumGrade.GetHashCode();
                if (this.ElevationHigh != null)
                    hashCode = hashCode * 59 + this.ElevationHigh.GetHashCode();
                if (this.ElevationLow != null)
                    hashCode = hashCode * 59 + this.ElevationLow.GetHashCode();
                if (this.StartLatlng != null)
                    hashCode = hashCode * 59 + this.StartLatlng.GetHashCode();
                if (this.EndLatlng != null)
                    hashCode = hashCode * 59 + this.EndLatlng.GetHashCode();
                if (this.ClimbCategory != null)
                    hashCode = hashCode * 59 + this.ClimbCategory.GetHashCode();
                if (this.City != null)
                    hashCode = hashCode * 59 + this.City.GetHashCode();
                if (this.State != null)
                    hashCode = hashCode * 59 + this.State.GetHashCode();
                if (this.Country != null)
                    hashCode = hashCode * 59 + this.Country.GetHashCode();
                if (this.Private != null)
                    hashCode = hashCode * 59 + this.Private.GetHashCode();
                if (this.AthletePrEffort != null)
                    hashCode = hashCode * 59 + this.AthletePrEffort.GetHashCode();
                if (this.CreatedAt != null)
                    hashCode = hashCode * 59 + this.CreatedAt.GetHashCode();
                if (this.UpdatedAt != null)
                    hashCode = hashCode * 59 + this.UpdatedAt.GetHashCode();
                if (this.TotalElevationGain != null)
                    hashCode = hashCode * 59 + this.TotalElevationGain.GetHashCode();
                if (this.Map != null)
                    hashCode = hashCode * 59 + this.Map.GetHashCode();
                if (this.EffortCount != null)
                    hashCode = hashCode * 59 + this.EffortCount.GetHashCode();
                if (this.AthleteCount != null)
                    hashCode = hashCode * 59 + this.AthleteCount.GetHashCode();
                if (this.Hazardous != null)
                    hashCode = hashCode * 59 + this.Hazardous.GetHashCode();
                if (this.StarCount != null)
                    hashCode = hashCode * 59 + this.StarCount.GetHashCode();
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
