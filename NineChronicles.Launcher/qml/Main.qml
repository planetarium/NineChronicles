import QtQuick 2.12
import QtQuick.Controls 2.2
import QtQuick.Window 2.12
import QtQuick.Layouts 1.1
import Qt.labs.platform 1.1

import LibplanetLauncher 1.0

Item {
    Timer {
        interval: 500
        running: true
        repeat: true

        property string logoPathTemplate: "../resources/images/logo-%1.png"
        property int logoIndex: 0
        property int logoFrameCount: 4

        onTriggered: {
            systemTrayIcon.icon.source = Qt.resolvedUrl(logoPathTemplate.arg(logoIndex))
            logoIndex = (logoIndex + 1) % logoFrameCount
        }
    }

    SystemTrayIcon {
        id: systemTrayIcon
        visible: true
        tooltip: "Libplanet Launcher, Not Flash (LLNF)"

        menu: Menu {
            MenuItem {
                id: runMenu
                text: "Run"
                visible: ctrl.privateKey != null && !ctrl.gameRunning && !ctrl.updating && !ctrl.preprocessing
                onTriggered: {
                    ctrl.runGameProcess()
                }
            }

            MenuItem {
                text: "Reload"
                visible: !ctrl.gameRunning
                onTriggered:{
                    ctrl.privateKey = null  // expect to login again
                    ctrl.stopSync()
                    passphraseWindow.show()
                }
            }

            MenuItem {
                text: "Settings"
                visible: !ctrl.gameRunning
                onTriggered: ctrl.openSettingFile()
            }

            MenuItem {
                text: "Quit"
                onTriggered: Qt.quit()
            }
        }
    }

    LibplanetController {
        id: ctrl
    }

    Window {
        id: passphraseWindow
        title: "Sign in"
        width: 320
        height: ctrl.keyStoreEmpty ? 210 : 170
        flags: Qt.FramelessWindowHint

        Column {
            padding: 10
            spacing: 5
            Label {
                 text: "Sign in to continue launcher."
            }

            Row {
                visible: !ctrl.keyStoreEmpty
                Label {
                    text: "Choose key:"
                    font.pixelSize: 12
                    rightPadding: 10
                }

                ComboBox {
                    id: addressComboBox
                    width: 200
                    model: Net.toListModel(ctrl.keyStoreOptions)
                }
            }

            Row {
                visible: ctrl.keyStoreEmpty
                Label {
                    text: "There are no key in the key store."
                }
            }

            Row {
                visible: ctrl.keyStoreEmpty
                Label {
                    text: "Create a new private key first:"
                }
            }

            Row {
                TextField {
                    id: passphraseInput
                    echoMode: TextInput.Password
                    placeholderText: ctrl.keyStoreEmpty ? "New passphrase" : "Your passphrase"
                }
            }

            Row {
                visible: ctrl.keyStoreEmpty
                TextField {
                    id: passphraseInputRetype
                    echoMode: TextInput.Password
                    placeholderText: "Retype passphrase"
                }
            }

            Row {
                Button {
                    text: ctrl.keyStoreEmpty ? "Create && &Sign in" : "&Sign in"
                    onClicked: {
                        const showError = (message) => {
                            if (!loginFailMessage.visible) {
                                passphraseWindow.height += 30
                                loginFailMessage.visible = true
                            }
                            loginFailMessage.text = message
                        }

                        let success = false;
                        if (ctrl.keyStoreEmpty) {
                            if (passphraseInput.text == '') {
                                showError("New passphrase is empty.");
                            }
                            else if (passphraseInputRetype.text == '') {
                                showError("Please retype passphrase.")
                            }
                            else if (passphraseInput.text != passphraseInputRetype.text) {
                                showError("Two passphrases do not match.");
                            }
                            else {
                                // TODO: passphrase strength 검사해야 함
                                ctrl.createPrivateKey(passphraseInput.text);
                                success = true;
                            }
                        }
                        else {
                            success = ctrl.login(addressComboBox.currentText, passphraseInput.text)
                            if (!success) {
                                showError("Passphrase seems wrong, try again.")
                            }
                        }

                        if (success) {
                            passphraseWindow.hide()
                            ctrl.startSync();
                        }
                    }
                }
            }

            Label {
                id: loginFailMessage
                visible: false
                color: "red"
            }
        }
    }

    Component.onCompleted: {
        passphraseWindow.show()
    }
}
