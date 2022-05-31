using CryptoRetriever.Strats;
using System.Collections.Generic;
using System.Windows;

namespace CryptoRetriever.UI {
    public class TreeUiEntry {
        public ITreeNode Node { get; set; }
        public ITreeNode Parent { get; set; }
        public int ChildIndexInParent { get; set; }
        public int Indentation { get; set; }

        public TreeUiEntry(ITreeNode node, ITreeNode parent, int childIndexInParent, int indentation) {
            Node = node;
            Parent = parent;
            ChildIndexInParent = childIndexInParent;
            Indentation = indentation;
        }

        /// <summary>
        /// Walks the tree in Pre-Order and builds a list of UiEntry nodes representing
        /// the order the UI should layout the blocks in. The indentation, parent,
        /// and child index are also recorded for later so the UI can properly indent
        /// each child.
        /// </summary>
        /// <param name="current">The current node being looked at.</param>
        /// <param name="parent">The parent of the current node or null if it's the first.</param>
        /// <param name="childIndex">The index the child is in the parent's list of children.</param>
        /// <param name="nodes">The list of nodes in the order they should appear.</param>
        /// <param name="indentation">The current indentation level.</param>
        public static void PreOrderTraverseNodes(ITreeNode current, ITreeNode parent, int childIndex, List<TreeUiEntry> nodes, int indentation) {
            nodes.Add(new TreeUiEntry(current, parent, childIndex, indentation));
            if (current.GetChildren() != null) {
                int index = 0;
                foreach (ITreeNode node in current.GetChildren()) {
                    PreOrderTraverseNodes(node, current, index, nodes, indentation + 1);
                    index++;
                }
            }
        }
    }

    public class TreeUiEntryClickHandlerBase {
        protected TreeUiEntry _entry;

        // Keek track of if the mouse was pressed on this
        // element so that we don't fire the even if you clicked on
        // something in a window over this button and then the up
        // event mis-fires.
        protected bool _isMouseDown = false;

        public TreeUiEntryClickHandlerBase(TreeUiEntry entry) {
            _entry = entry;
        }

        public virtual void OnMouseDown(object sender, RoutedEventArgs e) {
            _isMouseDown = true;
        }

        public virtual void OnMouseLeave(object sender, RoutedEventArgs e) {
            _isMouseDown = false;
        }

        public virtual void OnMouseUp(object sender, RoutedEventArgs e) {
            _isMouseDown = false;
        }
    }
}
