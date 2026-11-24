using System.Collections.Generic;
using TeachingContentSystem.Runtime.Core.Navigation;
using TeachingContentSystem.Runtime.Serialization;
using UnityEngine;

namespace TeachingContentSystem.Runtime.Core
{
    /// <summary>
    /// Drives the execution of a RuntimeContentGraph.
    /// Handles navigation, timing, block lifecycle, and analytics hooks.
    /// </summary>
    public class ContentManager : MonoBehaviour
    {
        public RuntimeContentGraph Graph { get; private set; }
        public RuntimeContentNode GetCurrentNode() => _nodes[_state.CurrentNodeId];
        public NavigationState GetState() => _state;


        private Dictionary<string, RuntimeContentNode> _nodes;
        private Dictionary<string, RuntimeContentBlock> _blocks;

        private NavigationState _state = new();
        private IContentBlock _activeBlock;

        // Das Ende wird je Besuch des letzten Knotens einmal gemeldet. Eine
        // Folie mit Dauer am Kursende wuerde sonst in jedem Bild erneut
        // "weiter" verlangen.
        private bool _endReported;

        /// <summary>
        /// "Weiter" wurde am letzten Baustein verlangt. Gleichbedeutend mit
        /// <see cref="NavigationEvents.OnCourseEndReached"/>, aber an diesen
        /// ContentManager gebunden. Der Wert ist die Kennung des Knotens.
        /// </summary>
        public event System.Action<string> CourseEndReached;

        // ---------------------------------------------------------
        // NAVIGATION STATE
        // ---------------------------------------------------------

        /// <summary>Der aktuelle Knoten hat keinen Nachfolger.</summary>
        public bool IsAtEnd
            => TryGetCurrent(out var node) && node.NextIds.Count == 0;

        /// <summary>Der aktuelle Knoten ist der Startknoten des Kurses.</summary>
        public bool IsAtStart
            => TryGetCurrent(out var node) && node.Id == Graph.StartNodeId;

        /// <summary>
        /// Ob "weiter" etwas tut: Es gibt einen Nachfolger, oder der Kurs
        /// beginnt mit <see cref="TcsSettings.WrapAtEnd"/> wieder von vorn.
        /// Eine Oberflaeche graut "weiter" aus, wenn das false ist.
        /// </summary>
        public bool CanGoNext
        {
            get
            {
                if (!TryGetCurrent(out var node)) return false;
                if (node.NextIds.Count > 0) return true;
                return TcsSettings.WrapAtEnd && WrapTarget(forward: true) != null;
            }
        }

        /// <summary>
        /// Ob "zurueck" etwas tut: Es gibt einen Verlauf, oder am Start fuehrt
        /// <see cref="TcsSettings.WrapAtEnd"/> zum letzten Baustein.
        /// </summary>
        public bool CanGoBack
        {
            get
            {
                if (!TryGetCurrent(out _)) return false;
                if (_state.History.Count > 0) return true;
                return TcsSettings.WrapAtEnd && IsAtStart && WrapTarget(forward: false) != null;
            }
        }

        private bool TryGetCurrent(out RuntimeContentNode node)
        {
            node = null;
            return _nodes != null
                && !string.IsNullOrEmpty(_state.CurrentNodeId)
                && _nodes.TryGetValue(_state.CurrentNodeId, out node);
        }

        /// <summary>
        /// Wohin ein Umlauf fuehrt: vorwaerts zum Startknoten, rueckwaerts
        /// zum letzten Knoten des letzten Kapitels. Null, wenn der Kurs nur
        /// aus dem aktuellen Knoten besteht.
        /// </summary>
        private string WrapTarget(bool forward)
        {
            if (Graph == null) return null;

            string target;
            if (forward)
            {
                target = Graph.StartNodeId;
            }
            else
            {
                RuntimeContentNode last = null;
                if (Graph.ChapterOrder.Count > 0)
                    Graph.LastNodeOfChapter.TryGetValue(
                        Graph.ChapterOrder[Graph.ChapterOrder.Count - 1], out last);
                if (last == null && Graph.Nodes.Count > 0)
                    last = Graph.Nodes[Graph.Nodes.Count - 1];
                target = last?.Id;
            }

            if (string.IsNullOrEmpty(target) || target == _state.CurrentNodeId) return null;
            return _nodes.ContainsKey(target) ? target : null;
        }

        // ---------------------------------------------------------
        // PUBLIC ENTRY POINT
        // ---------------------------------------------------------
        public void LoadGraph(RuntimeContentGraph graph)
        {
            Graph = graph;

            InitializeGraph();
            JsonValidator.Validate(Graph);
            EnterNode(Graph.StartNodeId, true);
        }

        // ---------------------------------------------------------
        // INTERNAL INITIALIZATION
        // ---------------------------------------------------------
        private void InitializeGraph()
        {
            _nodes = new();
            foreach (var node in Graph.Nodes)
                _nodes[node.Id] = node;

            _blocks = new();
            foreach (var block in Graph.Blocks)
                _blocks[block.Id] = block;
        }

        // ---------------------------------------------------------
        // UPDATE LOOP
        // ---------------------------------------------------------
        void Update()
        {
            if (_activeBlock == null)
                return;

            _state.TimeInNode += Time.deltaTime;
            _activeBlock.Update(Time.deltaTime);

            if (_activeBlock.Duration > 0 &&
                _state.TimeInNode >= _activeBlock.Duration)
            {
                GoToNext();
                return;
            }

            // Bausteine, die von sich aus fertig werden koennen: eine Aufgabe
            // in der Szene ist erledigt, wenn sie geloest ist, und das dauert
            // jedes Mal anders lang. Duration taugt dafuer nicht.
            //
            // Gefragt wird nur, wer es anbietet (ICompletableBlock), damit
            // vorhandene Bausteine unberuehrt bleiben.
            if (_activeBlock is ICompletableBlock fertig
                && fertig.AutoAdvance && fertig.IsComplete)
            {
                GoToNext();
            }
        }

        // ---------------------------------------------------------
        // NODE ENTRY / EXIT
        // ---------------------------------------------------------
        private void EnterNode(string nodeId, bool recordHistory)
        {
            if (!_nodes.TryGetValue(nodeId, out var node))
            {
                Debug.LogError($"Missing node: {nodeId}");
                return;
            }

            if (recordHistory && !string.IsNullOrEmpty(_state.CurrentNodeId))
                _state.History.Push(_state.CurrentNodeId);

            _state.CurrentNodeId = nodeId;
            _state.TimeInNode = 0;
            _endReported = false;

            NavigationEvents.OnNodeEntered?.Invoke(nodeId);

            var blockData = _blocks[node.BlockId];
            _activeBlock = BlockFactory.Create(blockData);

            NavigationEvents.OnBlockStarted?.Invoke(blockData.Id);
            _activeBlock.Enter();
        }

        private void ExitCurrentNode()
        {
            if (_activeBlock != null)
            {
                _activeBlock.Exit();
                NavigationEvents.OnBlockFinished?.Invoke(_activeBlock.Id);
            }

            NavigationEvents.OnNodeExited?.Invoke(_state.CurrentNodeId);
        }

        // ---------------------------------------------------------
        // NAVIGATION
        // ---------------------------------------------------------

        /// <summary>
        /// Zum naechsten Baustein. Am letzten Baustein ist das kein Fehler,
        /// sondern das Ende des Kurses: Es wird einmal gemeldet
        /// (<see cref="CourseEndReached"/>), und mit
        /// <see cref="TcsSettings.WrapAtEnd"/> geht es danach von vorn los.
        /// </summary>
        public void GoToNext()
        {
            if (!TryGetCurrent(out var current))
                return;

            if (current.NextIds.Count == 0)
            {
                if (!_endReported)
                {
                    _endReported = true;
                    NavigationEvents.OnCourseEndReached?.Invoke(current.Id);
                    CourseEndReached?.Invoke(current.Id);
                }

                if (!TcsSettings.WrapAtEnd)
                    return;

                string start = WrapTarget(forward: true);
                if (start == null)
                    return;

                ExitCurrentNode();
                EnterNode(start, true);
                return;
            }

            string nextId = current.NextIds[0];

            ExitCurrentNode();
            EnterNode(nextId, true);   // record history
        }

        /// <summary>
        /// Zurueck im Verlauf. Ohne Verlauf tut es nichts, ausser am Start
        /// mit <see cref="TcsSettings.WrapAtEnd"/>: dann zum letzten Baustein.
        /// </summary>
        public void GoBack()
        {
            if (_state.History.Count == 0)
            {
                string last = TcsSettings.WrapAtEnd && IsAtStart ? WrapTarget(forward: false) : null;
                if (last == null)
                    return;

                ExitCurrentNode();
                EnterNode(last, false);
                return;
            }

            string previous = _state.History.Pop();
            ExitCurrentNode();
            EnterNode(previous, false);   // do NOT record history but delete it
        }

        public void GoHome()
        {
            var current = _nodes[_state.CurrentNodeId];
            if (string.IsNullOrEmpty(current.HomeId))
            {
                Debug.LogWarning("No home node defined.");
                return;
            }

            ExitCurrentNode();
            EnterNode(current.HomeId, true);   //record history
        }
        // ---------------------------------------------------------
        // DIRECT JUMPS
        // ---------------------------------------------------------

        /// <summary>
        /// Jump to a specific node by global node ID.
        /// </summary>
        public bool GoToNode(string nodeId, bool recordHistory = true)
        {
            if (!_nodes.ContainsKey(nodeId))
            {
                Debug.LogError($"GoToNode: Unknown node {nodeId}");
                return false;
            }

            ExitCurrentNode();
            EnterNode(nodeId, recordHistory);
            return true;
        }

        /// <summary>
        /// Jump to a block by global block ID.
        /// </summary>
        public bool GoToBlock(string blockId, bool recordHistory = true)
        {
            string nodeId = blockId + "_node";
            return GoToNode(nodeId, recordHistory);
        }

        /// <summary>
        /// Jump to the first node of a chapter.
        /// </summary>
        public bool GoToChapter(string chapterName, bool recordHistory = true)
        {
            if (!Graph.FirstNodeOfChapter.TryGetValue(chapterName, out var firstNode))
            {
                Debug.LogError($"GoToChapter: Unknown chapter {chapterName}");
                return false;
            }

            return GoToNode(firstNode.Id, recordHistory);
        }

        /// <summary>
        /// Jump to a specific slide inside a chapter using local slide ID.
        /// </summary>
        public bool GoToSlide(string chapterName, string localSlideId, bool recordHistory = true)
        {
            string globalNodeId = $"{Graph.GraphId}.{chapterName}.{localSlideId}_node";
            return GoToNode(globalNodeId, recordHistory);
        }

    }
}
