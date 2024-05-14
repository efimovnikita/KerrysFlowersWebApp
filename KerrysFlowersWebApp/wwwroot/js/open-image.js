function openImageInNewTab(imageData) {
    const image = new Image();
    image.src = imageData;
    const w = window.open("");
    w.document.write(image.outerHTML);
}