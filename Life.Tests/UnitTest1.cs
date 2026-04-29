using cli_life;

namespace Life.Tests
{
    // Тесты для клеток
    public class CellTests
    {
        [Fact]
        public void AliveCell_WithTwoNeighbors_StaysAlive()
        {
            var cell = new Cell { IsAlive = true };
            cell.neighbors.Add(new Cell { IsAlive = true });
            cell.neighbors.Add(new Cell { IsAlive = true });
            cell.DetermineNextLiveState();
            cell.Advance();
            Assert.True(cell.IsAlive);
        }

        [Fact]
        public void AliveCell_WithThreeNeighbors_StaysAlive()
        {
            var cell = new Cell { IsAlive = true };
            cell.neighbors.Add(new Cell { IsAlive = true });
            cell.neighbors.Add(new Cell { IsAlive = true });
            cell.neighbors.Add(new Cell { IsAlive = true });
            cell.DetermineNextLiveState();
            cell.Advance();
            Assert.True(cell.IsAlive);
        }

        [Fact]
        public void AliveCell_WithOneNeighbor_Dies()
        {
            var cell = new Cell { IsAlive = true };
            cell.neighbors.Add(new Cell { IsAlive = true });
            cell.DetermineNextLiveState();
            cell.Advance();
            Assert.False(cell.IsAlive);
        }

        [Fact]
        public void DeadCell_WithThreeNeighbors_BecomesAlive()
        {
            var cell = new Cell { IsAlive = false };
            cell.neighbors.Add(new Cell { IsAlive = true });
            cell.neighbors.Add(new Cell { IsAlive = true });
            cell.neighbors.Add(new Cell { IsAlive = true });
            cell.DetermineNextLiveState();
            cell.Advance();
            Assert.True(cell.IsAlive);
        }
        [Fact]
        public void DeadCell_WithFourNeighbors_StaysDead()
        {
            var cell = new Cell { IsAlive = false };
            cell.neighbors.Add(new Cell { IsAlive = true });
            cell.neighbors.Add(new Cell { IsAlive = true });
            cell.neighbors.Add(new Cell { IsAlive = true });
            cell.neighbors.Add(new Cell { IsAlive = true });
            cell.DetermineNextLiveState();
            cell.Advance();
            Assert.False(cell.IsAlive);
        }
    }

    // Тесты для доски
    public class BoardTests
    {
        [Fact]
        public void NewBoard_HasCorrectSize()
        {
            var b = new Board(20, 10, 2, 0);
            Assert.Equal(10, b.Columns);
            Assert.Equal(5, b.Rows);
        }

        [Fact]
        public void BlockPattern_StaysUnchanged()
        {
            var b = new Board(10, 10, 1, 0);
            b.LoadPattern("patterns/block.txt", 2, 2);
            int initial = b.CountAlive();
            b.Advance(); b.Advance();
            Assert.Equal(initial, b.CountAlive());
        }

        [Fact]
        public void SaveAndLoad_Works()
        {
            var b = new Board(10, 10, 1, 0.3);
            string tmp = Path.GetTempFileName();
            b.Save(tmp);
            var b2 = Board.Load(tmp);
            Assert.Equal(b.CountAlive(), b2.CountAlive());
            File.Delete(tmp);
        }

        [Fact]
        public void GetComponents_Block_OneComponent()
        {
            var b = new Board(10, 10, 1, 0);
            b.LoadPattern("patterns/block.txt", 1, 1);
            var comps = b.GetComponents();
            Assert.Single(comps);
            Assert.Equal(4, comps[0].Count);
        }
        [Fact]
        public void PatternLoad_ClipsToBoardBounds()
        {
            var b = new Board(5, 5, 1, 0);
            b.LoadPattern("patterns/block.txt", 4, 4);
            Assert.True(b.Cells[4, 4].IsAlive);
            Assert.False(b.Cells[0, 0].IsAlive);
            Assert.Equal(1, b.CountAlive());
        }

        [Fact]
        public void ZeroDensity_NoAliveCells()
        {
            var b = new Board(10, 10, 1, 0.0);
            Assert.Equal(0, b.CountAlive());
        }
    }

    // Тесты для распознавателя устойчивых фигур
    public class RecognizerTests
    {
        [Fact]
        public void EmptyComponent_ClassifiedAsEmpty()
        {
            var empty = new List<CellPos>();
            Assert.Equal("Empty", PatternRecognizer.Classify(empty));
        }

        [Fact]
        public void Block_IsRecognized()
        {
            var b = new Board(10, 10, 1, 0);
            b.LoadPattern("patterns/block.txt", 2, 2);
            var comps = b.GetComponents();
            Assert.Equal("Block", PatternRecognizer.Classify(comps[0]));
        }

        [Fact]
        public void Beehive_IsRecognized()
        {
            var b = new Board(10, 10, 1, 0);
            b.LoadPattern("patterns/beehive.txt", 2, 2);
            var comps = b.GetComponents();
            Assert.Equal("Beehive", PatternRecognizer.Classify(comps[0]));
        }

        [Fact]
        public void Loaf_IsRecognized()
        {
            var b = new Board(10, 10, 1, 0);
            b.LoadPattern("patterns/loaf.txt", 2, 2);
            var comps = b.GetComponents();
            Assert.Equal("Loaf", PatternRecognizer.Classify(comps[0]));
        }

        [Fact]
        public void Boat_IsRecognized()
        {
            var b = new Board(10, 10, 1, 0);
            b.LoadPattern("patterns/boat.txt", 2, 2);
            var comps = b.GetComponents();
            Assert.Equal("Boat", PatternRecognizer.Classify(comps[0]));
        }

        [Fact]
        public void Tub_IsRecognized()
        {
            var b = new Board(10, 10, 1, 0);
            b.LoadPattern("patterns/tub.txt", 2, 2);
            var comps = b.GetComponents();
            Assert.Equal("Tub", PatternRecognizer.Classify(comps[0]));
        }

        [Fact]
        public void Unknown_IsReturnedForRandomGroup()
        {
            var b = new Board(10, 10, 1, 0);
            b.Cells[1, 1].IsAlive = true;
            b.Cells[1, 2].IsAlive = true;
            b.Cells[2, 1].IsAlive = true;
            var comps = b.GetComponents();
            Assert.Equal("Unknown", PatternRecognizer.Classify(comps[0]));
        }
    }
}
