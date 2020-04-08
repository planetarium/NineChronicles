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
            passphraseWindow.height = passphraseWindow.minimumHeight = passphraseWindow.maximumHeight = 200
            loginFailMessage.visible = true
        }
    }

    function runGame()
    {
        const succeed = ctrl.runGameProcess();
        if (!succeed)
        {
            showMessage("Failed to launch game.\nPlease re-install Nine Chronicles.");
            messageBox.onDestruction.connect(function() {
                Qt.Quit()
            })
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
                onTriggered: runGame()
            }

            MenuItem {
                id: loginMenu
                text: "Login"
                visible: ctrl.privateKey === null 

                onTriggered: {
                    passphraseWindow.show()
                    passphraseWindow.requestActivate()
                }
            }

            MenuItem {
                id: logoutMenu
                text: "Logout"
                visible: ctrl.privateKey !== null && !ctrl.gameRunning

                onTriggered: {
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
            if (reason == SystemTrayIcon.DoubleClick)
            {
                if (Qt.platform.os == "windows" && runMenu.visible)
                {
                    runGame()
                }
                else if (passphraseWindow.visible)
                {
                    passphraseWindow.requestActivate()
                }
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
        title: "Type your passphrase"
        width: 360
        height: 160
        minimumWidth: width
        minimumHeight: height
        maximumWidth: width
        maximumHeight: height
        flags: Qt.Dialog | Qt.WindowTitleHint | Qt.WindowCloseButtonHint

        Column {
            anchors.fill: parent
            anchors.margins: 10
            spacing: 10

            GridLayout
            {
                columns: 2
                width: parent.width

                Label {
                    text: "Address"
                    Layout.preferredWidth: 120
                    font.pointSize: 12
                }

                ComboBox {
                    id: addressComboBox
                    model: Net.toListModel(ctrl.keyStore.addresses)
                    Layout.fillWidth: true
                    font.pointSize: 12
                }

                Label {
                    text: "Passphrase"
                    Layout.preferredWidth: 120
                    font.pointSize: 12
                }
                
                TextField {
                    id: passphraseInput
                    echoMode: TextInput.Password
                    placeholderText: "Input passphrase"
                    onAccepted: login()
                    Layout.fillWidth: true
                    font.pointSize: 12
                }
            }

            Button {
                text: "Login"
                onClicked: login()
                width: parent.width;
                font.pointSize: 12
            }

            Label {
                id: loginFailMessage
                visible: false
                text: "Passphrase seems wrong, try again."
                color: "red"
                font.pointSize: 12
            }
        }
    }
    function showMessage(text)
    {
        messageBox.text = text;
        messageBox.visible = true;
    }

    Window {
        id: messageBox
        modality: Qt.ApplicationModal
        title: "Nine Chronicles Launcher"
        visible: false
        property alias text: messageBoxLabel.text
        minimumHeight: 100
        minimumWidth: 480
        maximumHeight: 100
        maximumWidth: 480
        flags: Qt.Dialog | Qt.WindowTitleHint | Qt.WindowCloseButtonHint

        Label {
            anchors.margins: 10
            anchors.fill: parent
            wrapMode: Text.WordWrap
            id: messageBoxLabel
            text: ""
            font.pointSize: 12
        }
    }

    Component.onCompleted: {
        passphraseWindow.show()
    }
}
