import { useRef, useState } from 'react';

export default function ExcelUpload({ onPreview, loading }) {
  const [dragOver, setDragOver] = useState(false);
  const inputRef = useRef(null);

  const handleFile = (file) => {
    if (!file) return;
    onPreview(file);
  };

  const onDrop = (e) => {
    e.preventDefault();
    setDragOver(false);
    const file = e.dataTransfer.files[0];
    handleFile(file);
  };

  return (
    <div
      className={`excel-upload ${dragOver ? 'drag-over' : ''} ${loading ? 'loading' : ''}`}
      onDragOver={(e) => { e.preventDefault(); setDragOver(true); }}
      onDragLeave={() => setDragOver(false)}
      onDrop={onDrop}
      onClick={() => inputRef.current?.click()}
    >
      <input
        ref={inputRef}
        type="file"
        accept=".xlsx,.xlsm,.xls,.pdf"
        hidden
        onChange={(e) => handleFile(e.target.files[0])}
      />
      <div className="upload-icon">📊</div>
      <p className="upload-title">{loading ? 'Parsing menu file...' : 'Drag & drop your weekly menu file'}</p>
      <p className="upload-hint">or click to browse (.xlsx, .xlsm, .xls, .pdf)</p>
    </div>
  );
}
