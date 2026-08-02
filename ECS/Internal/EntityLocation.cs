namespace EngineX.ECS
{
    internal struct EntityLocation
    {
        public Archetype Archetype;

        public Chunk Chunk;

        public int IndexInChunk;

        public int Version;
    }
}
