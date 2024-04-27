function initializeHorizontalScroll(element) {
    element.addEventListener('wheel', (event) => {
        event.preventDefault();

        element.scrollBy({
            left: event.deltaY < 0 ? -30 : 30,
        });
    });
}

window.initializeHorizontalScroll = initializeHorizontalScroll;