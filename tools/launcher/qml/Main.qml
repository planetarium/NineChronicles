import QtQuick 2.12
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
                visible: !ctrl.gameRunning
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
        }
    }

    LibplanetController {
        id: ctrl

        Component.onCompleted: {
            ctrl.startSync()
        }
    }
}
