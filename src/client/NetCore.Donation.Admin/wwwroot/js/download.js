window.downloadFile = (fileName, contentType, bytes) => {
    const payload = bytes instanceof Uint8Array ? bytes : new Uint8Array(bytes);
    const blob = new Blob([payload], { type: contentType || "application/pdf" });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = fileName;
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
    URL.revokeObjectURL(url);
};
