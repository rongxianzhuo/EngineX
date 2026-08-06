using EngineX.Baseline.Math;

namespace EngineX.Physics.Internal.Dynamics
{
    internal struct ContactPair
    {
        public BodyHandle BodyA;
        public BodyHandle BodyB;

        public ContactPair(BodyHandle a, BodyHandle b)
        {
            BodyA = a;
            BodyB = b;
        }
    }

    internal static class IslandBuilder
    {
        public const int MaxIslands = 64;
        public const int MaxIslandBodies = 256;

        public static int Build(
            BodySet bodies,
            ContactPair[] contacts,
            int contactCount,
            int[] parent,
            Island[] islands,
            int[] islandBodyList,
            int[] islandContactList)
        {
            int bodyCapacity = bodies.Capacity;
            for (int i = 0; i < bodyCapacity; i++) parent[i] = i;

            for (int c = 0; c < contactCount; c++)
            {
                BodyHandle a = contacts[c].BodyA;
                BodyHandle b = contacts[c].BodyB;
                if (!bodies.IsValid(a) || !bodies.IsValid(b)) continue;
                Union(parent, a.Id, b.Id);
            }

            int[] rootMap = new int[bodyCapacity];
            for (int i = 0; i < bodyCapacity; i++) rootMap[i] = -1;

            int islandCount = 0;
            int bodyListCursor = 0;
            int contactListCursor = 0;

            for (int i = 0; i < bodyCapacity; i++)
            {
                if (!bodies.IsValid(bodies.GetHandle(i))) continue;
                int root = Find(parent, i);
                int islandIdx = rootMap[root];
                if (islandIdx == -1)
                {
                    if (islandCount >= MaxIslands) break;
                    islandIdx = islandCount;
                    rootMap[root] = islandIdx;
                    islands[islandCount] = new Island
                    {
                        FirstBodyIndex = bodyListCursor,
                        BodyCount = 0,
                        FirstContactIndex = contactListCursor,
                        ContactCount = 0,
                    };
                    islandCount++;
                }
                if (bodyListCursor < islandBodyList.Length)
                {
                    islandBodyList[bodyListCursor++] = i;
                    islands[islandIdx].BodyCount++;
                }
            }

            for (int c = 0; c < contactCount; c++)
            {
                BodyHandle a = contacts[c].BodyA;
                BodyHandle b = contacts[c].BodyB;
                if (!bodies.IsValid(a) || !bodies.IsValid(b)) continue;
                int root = Find(parent, a.Id);
                int islandIdx = rootMap[root];
                if (islandIdx == -1) continue;
                if (contactListCursor < islandContactList.Length)
                {
                    islandContactList[contactListCursor++] = c;
                    islands[islandIdx].ContactCount++;
                }
            }

            return islandCount;
        }

        private static int Find(int[] parent, int x)
        {
            while (parent[x] != x)
            {
                parent[x] = parent[parent[x]];
                x = parent[x];
            }
            return x;
        }

        private static void Union(int[] parent, int a, int b)
        {
            int ra = Find(parent, a);
            int rb = Find(parent, b);
            if (ra != rb) parent[ra] = rb;
        }
    }
}