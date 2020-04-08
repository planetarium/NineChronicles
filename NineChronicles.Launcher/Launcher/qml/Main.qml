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
            ctrl.startSync()
            passphraseWindow.hide()
            preloadProgress.show()
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
        tooltip: ctrl.preprocessing ? ctrl.preloadStatus : "Nine Chronicles"

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
                else if (ctrl.preprocessing)
                {
                    preloadProgress.show()
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
        id: preloadProgress
        title: "Nine Chronicles"
        width: 240
        height: 40
        minimumWidth: width
        minimumHeight: height
        maximumWidth: width
        maximumHeight: height
        flags: Qt.Dialog | Qt.WindowTitleHint | Qt.WindowCloseButtonHint
        visible: false

        onClosing: {
            // https://doc.qt.io/qt-5/qguiapplication.html#quitOnLastWindowClosed-prop 를 설정할 방법이 없어
            // 가려만 둡니다.
            close.accepted = false
            preloadProgress.hide()
        }
        ColumnLayout{
            spacing: 1
            anchors.fill: parent
            anchors.margins: 10

            ProgressBar {
                indeterminate: true
                Layout.preferredWidth: parent.width
                visible: ctrl.preprocessing
            }
            
            Label {
                text: ctrl.preprocessing ? ctrl.preloadStatus : "Done!"
                Layout.preferredWidth: parent.width
                visible: ctrl.preprocessing
            }

            Button {
                text: "Play Nine Chronicles"
                Layout.preferredWidth: parent.width
                Layout.preferredHeight: 20
                visible: !ctrl.preprocessing
                onClicked: {
                    preloadProgress.hide()
                    runGame()
                }
            }
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
