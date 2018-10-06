mergeInto(LibraryManager.library, {

  OnLoadUnity: function () {
    window.onLoadUnity();
  },

  OnMessage: function (msg) {
    window.onMessage(msg)
  }
});
