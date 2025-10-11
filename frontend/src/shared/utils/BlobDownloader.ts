export const downloadBlob = (
    blob: Blob,
    defaultFilename: string,
    contentDisposition?: string
) => {
    let filename = defaultFilename;

    if (contentDisposition) {
        const match = contentDisposition.match(/filename=([^;]+)/);
        if (match) {
            filename = match[1].replace(/['"]/g, "");
        }
    }
    // Create download
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.setAttribute("download", filename);
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.URL.revokeObjectURL(url);
};
