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

        public void GoToNext()
        {
            var current = _nodes[_state.CurrentNodeId];

            if (current.NextIds.Count == 0)
            {
                Debug.LogError($"Node {current.Id} has no NextIds");
                return;
            }

            string nextId = current.NextIds[0];

            ExitCurrentNode();
            EnterNode(nextId, true);   // record history
        }

        public void GoBack()
        {
            if (_state.History.Count == 0)
            {
                Debug.LogWarning("No history to go back to.");
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
