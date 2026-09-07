import { useState } from 'react';

export default function MenuTree({ nodes, onChange, editable = false, defaultExpanded = true }) {
  if (!nodes?.length) {
    return <p className="empty-tree">No menu items yet.</p>;
  }

  return (
    <ul className={`menu-tree ${editable ? 'editable' : ''}`}>
      {nodes.map((node, index) => (
        <MenuTreeNode
          key={node.id ?? `node-${index}`}
          node={node}
          path={[index]}
          onChange={onChange}
          editable={editable}
          defaultExpanded={defaultExpanded}
        />
      ))}
    </ul>
  );
}

function MenuTreeNode({ node, path, onChange, editable, defaultExpanded }) {
  const [expanded, setExpanded] = useState(defaultExpanded);
  const hasChildren = node.children?.length > 0;

  const updateNode = (updates) => {
    if (!onChange) return;
    onChange(path, { ...node, ...updates });
  };

  const addChild = () => {
    const children = [...(node.children || []), { label: 'New item', sortOrder: node.children?.length || 0, children: [] }];
    updateNode({ children });
    setExpanded(true);
  };

  const removeNode = () => onChange(path, null);

  const updateChild = (childPath, childNode) => {
    const children = [...(node.children || [])];
    if (childNode === null) {
      children.splice(childPath[childPath.length - 1], 1);
    } else {
      let target = children;
      for (let i = 1; i < childPath.length - 1; i++) {
        target = target[childPath[i]].children;
      }
      target[childPath[childPath.length - 1]] = childNode;
    }
    updateNode({ children });
  };

  const depthClass = path.length === 1 ? 'meal' : path.length === 2 ? 'category' : 'item';

  return (
    <li className={`menu-tree-node ${depthClass}`}>
      <div className="menu-tree-row">
        {hasChildren && (
          <button type="button" className="tree-toggle" onClick={() => setExpanded(!expanded)} aria-label={expanded ? 'Collapse' : 'Expand'}>
            {expanded ? '▼' : '▶'}
          </button>
        )}
        {!hasChildren && <span className="tree-spacer" />}
        {editable ? (
          <input
            className="tree-label-input"
            value={node.label}
            onChange={(e) => updateNode({ label: e.target.value })}
            placeholder="Item name"
          />
        ) : (
          <span className="tree-label">{node.label}</span>
        )}
        {editable && (
          <div className="tree-actions">
            <button type="button" className="btn-icon" onClick={addChild} title="Add child">+</button>
            <button type="button" className="btn-icon danger" onClick={removeNode} title="Remove">×</button>
          </div>
        )}
      </div>
      {hasChildren && expanded && (
        <ul className="menu-tree-children">
          {node.children.map((child, index) => (
            <MenuTreeNode
              key={child.id ?? `child-${index}`}
              node={child}
              path={[...path, index]}
              onChange={updateChild}
              editable={editable}
              defaultExpanded={defaultExpanded}
            />
          ))}
        </ul>
      )}
    </li>
  );
}
