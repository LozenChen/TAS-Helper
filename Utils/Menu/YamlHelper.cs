using YamlDotNet.Serialization;
using YamlDotNet.Serialization.ObjectFactories;

namespace Celeste.Mod.TASHelper.Utils.Menu {

    // need YamlDotNet ver >= 9 to support this
    // note this only works for non public properties, but not non public fields
    public static class TH_YamlHelper {
        public static ISerializer Serializer = new SerializerBuilder().IncludeNonPublicProperties().ConfigureDefaultValuesHandling(DefaultValuesHandling.Preserve).Build();


        public static IDeserializer Deserializer = new DeserializerBuilder().IncludeNonPublicProperties().IgnoreUnmatchedProperties().Build();

        public static IDeserializer DeserializerUsing(object objectToBind) {
            IObjectFactory defaultObjectFactory = new DefaultObjectFactory();
            Type objectType = objectToBind.GetType();
            return new DeserializerBuilder().IncludeNonPublicProperties().IgnoreUnmatchedProperties().WithObjectFactory((Type type) => (!(type == objectType)) ? defaultObjectFactory.Create(type) : objectToBind).Build();
        }
    }
}