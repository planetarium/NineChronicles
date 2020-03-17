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
                visible: !ctrl.gameRunning && !ctrl.updating && !ctrl.preprocessing
                onTriggered: {
                    ctrl.runGame()
                }
            }

            MenuItem {
                text: "Reload"
                visible: !ctrl.gameRunning
                onTriggered:{
                    ctrl.stopSync()
                    ctrl.startSync()
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

            MenuItem {
                text: "test"
                onTriggered: console.log(loader.item.open())
            }
        }
    }

    LibplanetController {
        id: ctrl
    }

    Window {
        id: passphraseWindow
        title: "Input passphrase"
        width: 320
        height: 80
        flags: Qt.FramelessWindowHint

        Column {
            padding: 10
            spacing: 5
            Label {
                 text: "Login is needed to continue launcher"
            }

            Row {
                TextField {
                    id: passphraseInput
                    echoMode: TextInput.Password
                    placeholderText: "Input passphrase"
                }
                Button {
                    text: "login"
                    onClicked: {
                        const success = ctrl.login(passphraseInput.text)
                        if (success) {
                            passphraseWindow.close()
                            ctrl.startSync();
                        }
                        else {
                            passphraseWindow.height = 110
                            loginFailMessage.visible = true
                        }
                    }
                }
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
