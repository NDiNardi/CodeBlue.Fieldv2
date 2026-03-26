window.codeBlue = window.codeBlue || {};
window.codeBlue.fileInputs = window.codeBlue.fileInputs || {};

window.codeBlue.fileInputs.open = function (containerId) {
  try {
    const container = document.getElementById(containerId);
    const input = container ? container.querySelector('input[type="file"]') : null;

    if (!input) {
      console.warn("codeBlue.fileInputs.open could not find file input:", containerId);
      return;
    }

    input.click();
  } catch (e) {
    console.error("codeBlue.fileInputs.open failed:", e);
  }
};
