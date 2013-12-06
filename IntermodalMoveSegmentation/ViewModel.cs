﻿using QuickGraph;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuickGraph.Serialization;
using QuickGraph.Algorithms.ShortestPath;
using System.Xml;

namespace IntermodalMoveSegmentation
{
    public class ViewModel : INotifyPropertyChanged
    {
        public ViewModel()
        {
            LayoutAlgorithmType = layoutAlgorithmTypes[0];
            ComputeAllPaths();
        }

        public MyGraph Graph
        {
            get { return graph; }
            set { graph = value; OnPropertyChanged("Graph"); }
        }
        private MyGraph graph;

        public string AllPaths { get; private set; }
        public int PathCount { get; private set; }
        public string TransitionNodes { get; private set; }
        public string TransitionEdges { get; private set; }
        public void ComputeAllPaths()
        {
            Graph = MyGraphSerializeHelper.LoadDGML("IntermodalGraph.dgml");
            var fw = new FloydWarshallAllShortestPathAlgorithm<MyVertex, MyEdge>(Graph, e => 1);
            fw.Compute();
            Func<MyVertex, MyVertex, IEnumerable<MyEdge>> getPath = (source, target) =>
            {
                IEnumerable<MyEdge> path;
                fw.TryGetPath(source, target, out path);
                return path;
            };
            Func<IEnumerable<MyEdge>, string> pathToString = path => string.Join("->", path.Select(e => e.Source).Concat(new[] { path.Last().Target }));

            var paths = (from source in Graph.Vertices.Where(v => !Graph.IsOutEdgesEmpty(v))
                         from target in Graph.Vertices.Where(v => Graph.IsOutEdgesEmpty(v))
                         where source != target && getPath(source, target) != null
                         select "<" + string.Join("|", getPath(source, target).Select(e => e.Abbreviation)) + ">\t\t"
                         + source.ID + " To " + target.ID + ": "
                         + pathToString(getPath(source, target)));
            AllPaths = string.Join(Environment.NewLine, paths);
            PathCount = paths.Count();

            TransitionNodes = string.Join("," + Environment.NewLine,
                Graph.Vertices.Select(v => v.CodeString()).OrderBy(s => s));
            TransitionEdges = string.Join("," + Environment.NewLine,
                Graph.Edges.Select(e =>
                "new TransitionEdge(TransitionNode."
                + e.Source.CodeString()
                + ", TransitionNode."
                + e.Target.CodeString()
                + ", \"" + e.Abbreviation + "\")"));
        }

        public IEnumerable<string> LayoutAlgorithmTypes { get { return layoutAlgorithmTypes; } }
        private string[] layoutAlgorithmTypes = new[] {
            "BoundedFR", "Circular", "CompoundFDP", "EfficientSugiyama", "FR", "ISOM", "KK", "LinLog", "Tree"
        };

        public string LayoutAlgorithmType
        {
            get { return layoutAlgorithmType; }
            set { layoutAlgorithmType = value; OnPropertyChanged("LayoutAlgorithmType"); }
        }
        private string layoutAlgorithmType;

        public string Summary
        {
            get { return summary; }
            set { summary = value; OnPropertyChanged("Summary"); }
        }
        private string summary;

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        #endregion
    }
}