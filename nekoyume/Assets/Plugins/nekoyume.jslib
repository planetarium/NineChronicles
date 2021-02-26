mergeInto(LibraryManager.library, {

  OnLoadUnity: function () {
    window.onLoadUnity();
  },

  OnMessage: function (msg) {
    window.onMessage(Pointer_stringify(msg))
  },

  OnSkill: function (name) {
    window.onSkill(Pointer_stringify(name))
  }
});
