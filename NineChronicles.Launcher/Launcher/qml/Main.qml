import QtQuick 2.12
import QtQuick.Controls 2.2
import QtQuick.Window 2.12
import QtQuick.Layouts 1.1
import Qt.labs.platform 1.1
import QtQuick.Controls.Styles 1.4

import LibplanetLauncher 1.0

Item {
    function login() {
        const showError = (message) => {
            if (!loginFailMessage.visible) {
                passphraseWindow.maximumHeight += 30
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
                ctrl.startSync()
                passphraseWindow.hide()
                preloadProgress.show()
            }
        }
        else {
            const success = ctrl.login(addressComboBox.currentText, passphraseInput.text)
            if (!success) {
                showError("Passphrase seems wrong, try again.")
            } else {
                ctrl.startSync()
                passphraseWindow.hide()
                preloadProgress.show()
            }
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
        running: ctrl.preprocessing
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
        icon.source: Qt.resolvedUrl("../images/logo-0.png")

        menu: Menu {
            MenuItem {
                id: peerAddress
                visible: (ctrl.privateKey != null &&
                          !ctrl.updating &&
                          !ctrl.preprocessing &&
                          ctrl.currentNodeAddress != null)
                text: "My node: " + ctrl.currentNodeAddress
                onTriggered: ctrl.copyClipboard(ctrl.currentNodeAddress)
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

            Menu {
                title: "Advanced…"
                visible: !ctrl.gameRunning

                MenuItem {
                    text: "Settings"
                    onTriggered: ctrl.openSettingFile()
                }

                MenuItem {
                    text: "Clear cache"
                    onTriggered: ctrl.clearStore()
                }

                MenuItem {
                    text: "Download the latest blockchain snapshot"
                    onTriggered: ctrl.downloadBlockchainSnapshot()
                }

                Menu {
                    id: keyRevokationMenu
                    title: "Revoke key…"
                    visible: !ctrl.gameRunning && !ctrl.keyStoreEmpty

                    Instantiator {
                        model: Net.toListModel(ctrl.keyStoreOptions)

                        MenuItem {
                            text: modelData
                            onTriggered: {
                                const addressHex = text
                                showMessage(
                                    `Revokes the private key corresponding to the address ${addressHex}.`,
                                    () => ctrl.revokeKey(addressHex)
                                )
                            }
                        }

                        onObjectAdded: keyRevokationMenu.insertItem(index, object)
                        onObjectRemoved: keyRevokationMenu.removeItem(object)
                    }
                }
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

        Component.onCompleted: {
            ctrl.quit.connect(function() {
                ctrl.stopGameProcess()
                ctrl.stopSync()
                Qt.quit()
            })

            ctrl.fatalError.connect((message, retryable) => {
                showMessage(
                    message,
                    // onClosing
                    () => {
                        ctrl.quit()
                    },
                    // onRetrying
                    // fatalError가 IBD 실패 상황에서만 호출되는 걸 가정.
                    retryable ? () => {
                        ctrl.stopSync()
                        ctrl.startSync()
                    } : undefined
                )
            })
        }
    }

    Window {
        id: preloadProgress
        title: "Nine Chronicles"
        width: 480
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
        title: "Sign in"
        width: 360
        height: ctrl.keyStoreEmpty ? 230 : 155
        minimumWidth: width
        minimumHeight: height
        maximumWidth: width
        maximumHeight: height
        flags: Qt.Dialog | Qt.WindowTitleHint | Qt.WindowCloseButtonHint

        Column {
            anchors.fill: parent
            anchors.margins: 10
            spacing: 10

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

            GridLayout
            {
                columns: 2
                width: parent.width

                Label {
                    text: "Address"
                    Layout.preferredWidth: 120
                }

                ComboBox {
                    visible: !ctrl.keyStoreEmpty
                    id: addressComboBox
                    model: Net.toListModel(ctrl.keyStoreOptions)
                    Layout.fillWidth: true
                }

                Label {
                    visible: ctrl.keyStoreEmpty
                    Component.onCompleted: {
                        text = ctrl.preparedPrivateKeyAddressHex.substr(0, 24) + "…"
                    }
                }

                Label {
                    text: "Passphrase"
                    Layout.preferredWidth: 120
                }

                TextField {
                    id: passphraseInput
                    echoMode: TextInput.Password
                    placeholderText: ctrl.keyStoreEmpty ? "New passphrase" : "Your passphrase"
                    onAccepted: login()
                    Layout.fillWidth: true
                }

                Label {
                    visible: ctrl.keyStoreEmpty
                    text: "Retype Passphrase"
                    Layout.preferredWidth: 120
                }

                TextField {
                    visible: ctrl.keyStoreEmpty
                    id: passphraseInputRetype
                    echoMode: TextInput.Password
                    placeholderText: "Retype passphrase"
                    onAccepted: login()
                    Layout.fillWidth: true
                }
            }

            Button {
                text: ctrl.keyStoreEmpty ? "Create && &Sign in" : "&Sign in"
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

    function showMessage(text, onClosing, onRetrying)
    {
        messageBox.text = text;
        messageBox.visible = true;
        retryButton.visible = typeof onRetrying !== 'undefined';
        if (typeof onRetrying !== 'undefined') {
            retryButton.onRetrying = () => {
                messageBox.visible = false;
                onRetrying();
            };
        }
        if (typeof onClosing !== 'undefined') {
            messageBox.onClosing.connect(() => onClosing())
        }
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

        ColumnLayout{
            spacing: 1
            anchors.fill: parent
            anchors.margins: 10

            Label {
                Layout.preferredWidth: parent.width
                wrapMode: Text.WordWrap
                id: messageBoxLabel
                text: ""
            }

            RowLayout {
                Layout.preferredHeight: 20
                Layout.alignment: Qt.AlignCenter
                spacing: 2
                Button {
                    text: "Retry"
                    id: retryButton
                    property var onRetrying: () => { console.log("onRetrying was not assigned yet.") }
                    visible: typeof onClicked !== "undefined"
                    onClicked: {
                        onRetrying()
                    }
                    Layout.alignment: Qt.AlignCenter
                }

                Button {
                    text: "Close"
                    onClicked: {
                        messageBox.close()
                    }
                    Layout.alignment: Qt.AlignCenter
                }
            }
        }
    }

    Window {
        id: snapshotDownloadProgress
        title: "Nine Chronicles"
        width: 480
        height: 40
        minimumWidth: width
        minimumHeight: height
        maximumWidth: width
        maximumHeight: height
        flags: Qt.Dialog | Qt.WindowTitleHint | Qt.WindowCloseButtonHint
        visible: ctrl.downloadingBlockchainSnapshot

        ColumnLayout{
            spacing: 1
            anchors.fill: parent
            anchors.margins: 10

            ProgressBar {
                indeterminate: false
                value: ctrl.blockchainSnapshotDownloadProgress
                Layout.preferredWidth: parent.width
                visible: true
            }

            Label {
                text: "Downloading the latest blockchain snapshot…"
                Layout.preferredWidth: parent.width
                visible: true
            }
        }
    }

    Window {
        id: popup
        width: 400
        ColumnLayout {
            anchors.fill: parent
            anchors.margins: 10

            Label {
                id: popupLabel
                text: ""
                Layout.alignment: Qt.AlignCenter
            }

            Button {
                id: popupButton
                text: "Create Account"
                property var onClickEvent
                onClicked: {
                   popup.visible = false
                   onClickEvent()
                }
                Layout.alignment: Qt.AlignCenter
            }
        }

        function show(text, btnMsg, next) {
            popupLabel.text = text
            popupButton.text = btnMsg
            popupButton.onClickEvent = next
            popup.height = 10 + text.split('\n').length * 18 + 60 + 10
            popup.visible = true
        }
    }

    Component.onCompleted: {
        if (ctrl.keyStoreEmpty) {
            popup.show(ctrl.welcomeMessage, "Create Account", () => {
                popup.show("ID was created successfully!\n\nClick the below button and go to complete sign up steps!", "Create Password", passphraseWindow.show)
            })
        } else {
            passphraseWindow.show()
        }
    }
}
