import QtQuick 2.12
import QtQuick.Controls 2.2
import QtQuick.Window 2.12
import QtQuick.Layouts 1.1
import Qt.labs.platform 1.1

import LibplanetLauncher 1.0

Item {
    function login() {
        const success = ctrl.login(addressComboBox.currentText, passphraseInput.text)
        if (success) {
            passphraseWindow.hide()
            ctrl.startSync();
        }
        else {
            passphraseWindow.height = 160
            loginFailMessage.visible = true
        }
    }

    Timer {
        interval: 500
        running: true
        repeat: true

        property string logoPathTemplate: "../images/logo-%1.png"
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
        tooltip: ctrl.tooltipText

        menu: Menu {
            MenuItem {
                id: peerAddress
                visible: (ctrl.privateKey != null &&
                          !ctrl.gameRunning &&
                          !ctrl.updating &&
                          !ctrl.preprocessing &&
                          ctrl.currentNodeAddress != null)
                text: "My node: " + ctrl.currentNodeAddress
                // FIXME: 누르면 클립보드에 주소 복사하게...
            }

            MenuSeparator { }

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

        onActivated: {
            if (reason == SystemTrayIcon.DoubleClick && Qt.platform.os == "windows" && runMenu.visible)
            {
                ctrl.runGameProcess()
            }
        }
    }

    LibplanetController {
        id: ctrl

        Component.onDestruction: {
            ctrl.stopGameProcess()
        }
    }

    Window {
        id: passphraseWindow
        title: "Input passphrase"
        width: 640
        height: 130
        minimumWidth: 640
        minimumHeight: 240
        maximumWidth: 640
        maximumHeight: 240
        flags: Qt.Tool

        Column {
            anchors.fill: parent
            anchors.margins: 20
            spacing: 10

            GridLayout
            {
                id: grid
                columns: 2
                width: parent.width

                Label {
                    text: "Address"
                    Layout.preferredWidth: 180
                }

                ComboBox {
                    id: addressComboBox
                    model: Net.toListModel(ctrl.keyStore.addresses)
                    Layout.fillWidth: true
                }

                Label {
                    text: "Passphrase"
                    Layout.preferredWidth: 180
                }
                
                TextField {
                    id: passphraseInput
                    echoMode: TextInput.Password
                    placeholderText: "Input passphrase"
                    onAccepted: login()
                    Layout.fillWidth: true
                }
            }

            Button {
                text: "Login"
                onClicked: login()
                width: parent.width;
            }

            Label {
                id: loginFailMessage
                visible: false
                text: "Passphrase seems wrong, try again."
                color: "red"
            }
        }
    }

    Component.onCompleted: {
        passphraseWindow.show()
    }
}
