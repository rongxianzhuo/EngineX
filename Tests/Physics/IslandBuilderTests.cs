using EngineX.Baseline.FixedPoint;
using EngineX.Baseline.Math;
using EngineX.Physics;
using EngineX.Physics.Internal.Dynamics;

namespace EngineX.Physics.Tests
{
    public static class IslandBuilderTests
    {
        [Test]
        public static void EmptyContactGraphMakesSingletonIslands()
        {
            var bodies = new BodySet(16);
            var handles = new BodyHandle[5];
            for (int i = 0; i < 5; i++)
            {
                handles[i] = bodies.Add(Body.Defaults);
            }

            var contacts = new ContactPair[0];
            var parent = new int[16];
            var islands = new Island[IslandBuilder.MaxIslands];
            var bodyList = new int[16];
            var contactList = new int[16];

            int count = IslandBuilder.Build(
                bodies, contacts, 0, parent, islands, bodyList, contactList);

            TestRunner.AssertEqual(5, count, "5 bodies with no contacts = 5 islands");
        }

        [Test]
        public static void TwoBodiesOneContactFormsOneIsland()
        {
            var bodies = new BodySet(16);
            var hA = bodies.Add(Body.Defaults);
            var hB = bodies.Add(Body.Defaults);
            var contacts = new ContactPair[1];
            contacts[0] = new ContactPair(hA, hB);
            var parent = new int[16];
            var islands = new Island[IslandBuilder.MaxIslands];
            var bodyList = new int[16];
            var contactList = new int[16];

            int count = IslandBuilder.Build(
                bodies, contacts, 1, parent, islands, bodyList, contactList);

            TestRunner.AssertEqual(1, count, "two bodies + one contact = one island");
            TestRunner.AssertEqual(2, islands[0].BodyCount, "island has 2 bodies");
            TestRunner.AssertEqual(1, islands[0].ContactCount, "island has 1 contact");
        }

        [Test]
        public static void ChainOfThreeBodiesIsOneIsland()
        {
            var bodies = new BodySet(16);
            var hA = bodies.Add(Body.Defaults);
            var hB = bodies.Add(Body.Defaults);
            var hC = bodies.Add(Body.Defaults);

            var contacts = new ContactPair[2];
            contacts[0] = new ContactPair(hA, hB);
            contacts[1] = new ContactPair(hB, hC);

            var parent = new int[16];
            var islands = new Island[IslandBuilder.MaxIslands];
            var bodyList = new int[16];
            var contactList = new int[16];

            int count = IslandBuilder.Build(
                bodies, contacts, 2, parent, islands, bodyList, contactList);

            TestRunner.AssertEqual(1, count, "chain = one island");
            TestRunner.AssertEqual(3, islands[0].BodyCount, "island has all 3 bodies");
            TestRunner.AssertEqual(2, islands[0].ContactCount, "island has 2 contacts");
        }

        [Test]
        public static void TwoSeparatePairsFormTwoIslands()
        {
            var bodies = new BodySet(16);
            var hA = bodies.Add(Body.Defaults);
            var hB = bodies.Add(Body.Defaults);
            var hC = bodies.Add(Body.Defaults);
            var hD = bodies.Add(Body.Defaults);

            var contacts = new ContactPair[2];
            contacts[0] = new ContactPair(hA, hB);
            contacts[1] = new ContactPair(hC, hD);

            var parent = new int[16];
            var islands = new Island[IslandBuilder.MaxIslands];
            var bodyList = new int[16];
            var contactList = new int[16];

            int count = IslandBuilder.Build(
                bodies, contacts, 2, parent, islands, bodyList, contactList);

            TestRunner.AssertEqual(2, count, "two separate pairs = 2 islands");
            int bodyCountSum = islands[0].BodyCount + islands[1].BodyCount;
            int contactCountSum = islands[0].ContactCount + islands[1].ContactCount;
            TestRunner.AssertEqual(4, bodyCountSum, "total 4 bodies across 2 islands");
            TestRunner.AssertEqual(2, contactCountSum, "total 2 contacts across 2 islands");
        }

        [Test]
        public static void StaleHandlesAreIgnored()
        {
            var bodies = new BodySet(16);
            var hA = bodies.Add(Body.Defaults);
            var hB = bodies.Add(Body.Defaults);
            bodies.Remove(hB);

            var contacts = new ContactPair[1];
            contacts[0] = new ContactPair(hA, hB);

            var parent = new int[16];
            var islands = new Island[IslandBuilder.MaxIslands];
            var bodyList = new int[16];
            var contactList = new int[16];

            int count = IslandBuilder.Build(
                bodies, contacts, 1, parent, islands, bodyList, contactList);

            TestRunner.AssertEqual(1, count, "stale handle should be ignored, 1 island with just hA");
            TestRunner.AssertEqual(1, islands[0].BodyCount, "only the live body in the island");
            TestRunner.AssertEqual(0, islands[0].ContactCount, "no contacts since the pair was stale");
        }

        [Test]
        public static void MultipleContactsSamePair()
        {
            var bodies = new BodySet(16);
            var hA = bodies.Add(Body.Defaults);
            var hB = bodies.Add(Body.Defaults);

            var contacts = new ContactPair[3];
            contacts[0] = new ContactPair(hA, hB);
            contacts[1] = new ContactPair(hA, hB);
            contacts[2] = new ContactPair(hA, hB);

            var parent = new int[16];
            var islands = new Island[IslandBuilder.MaxIslands];
            var bodyList = new int[16];
            var contactList = new int[16];

            int count = IslandBuilder.Build(
                bodies, contacts, 3, parent, islands, bodyList, contactList);

            TestRunner.AssertEqual(1, count, "multiple contacts same pair = 1 island");
            TestRunner.AssertEqual(2, islands[0].BodyCount);
            TestRunner.AssertEqual(3, islands[0].ContactCount, "all 3 contacts in the same island");
        }

        [Test]
        public static void IslandBodyListContainsAllBodies()
        {
            var bodies = new BodySet(16);
            for (int i = 0; i < 4; i++) bodies.Add(Body.Defaults);
            var hA = bodies.GetHandle(0);
            var hB = bodies.GetHandle(1);

            var contacts = new ContactPair[1];
            contacts[0] = new ContactPair(hA, hB);

            var parent = new int[16];
            var islands = new Island[IslandBuilder.MaxIslands];
            var bodyList = new int[16];
            var contactList = new int[16];

            int count = IslandBuilder.Build(
                bodies, contacts, 1, parent, islands, bodyList, contactList);

            TestRunner.AssertEqual(3, count, "3 islands: A-B paired, 2 singletons");

            int pairedBodyCount = 0;
            int pairedBodyListStart = -1;
            for (int i = 0; i < count; i++)
            {
                if (islands[i].BodyCount == 2)
                {
                    pairedBodyCount = islands[i].BodyCount;
                    pairedBodyListStart = islands[i].FirstBodyIndex;
                }
            }
            TestRunner.AssertEqual(2, pairedBodyCount, "one island with 2 bodies");
            TestRunner.Assert(pairedBodyListStart >= 0);
            int b0 = bodyList[pairedBodyListStart];
            int b1 = bodyList[pairedBodyListStart + 1];
            TestRunner.Assert((b0 == 0 && b1 == 1) || (b0 == 1 && b1 == 0),
                "paired island should contain bodies 0 and 1");
        }
    }
}