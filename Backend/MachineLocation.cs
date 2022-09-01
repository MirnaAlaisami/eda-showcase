public partial class MachineLocation
{
    public MachineLocation()
    {
        Stuttgart = false;
        Hamburg = false;
        Frankfurt = false;
    }
    [Newtonsoft.Json.JsonProperty("stuttgart", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
    public bool Stuttgart { get; set; }
    [Newtonsoft.Json.JsonProperty("hamburg", Required = Newtonsoft.Json.Required.AllowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
    public bool Hamburg { get; set; }
    [Newtonsoft.Json.JsonProperty("frankfurt", Required = Newtonsoft.Json.Required.AllowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
    public bool Frankfurt { get; set; }
}