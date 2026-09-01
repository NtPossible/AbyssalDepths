namespace AbyssalDepths.src.Config
{
    public class AbyssalDepthsConfig
    {
        public bool EnablePressure = true;
        public bool EnableSchematicCrafting = false;

        // Legacy settings for people who want to use the old pressure system
        public bool EnableLegacyPressure = false;
        public int LegacyBaseSafeDepth = 10;
    }
}