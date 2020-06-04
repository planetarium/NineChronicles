//
//  AppDelegate.swift
//  Notifier
//
//  Created by Lee Dogeon on 27/05/2020.
//  Copyright © 2020 Planetarium. All rights reserved.
//

import Cocoa

@NSApplicationMain
class AppDelegate: NSObject, NSApplicationDelegate, NSUserNotificationCenterDelegate {

    func applicationDidFinishLaunching(_ aNotification: Notification) {
        let NSApplicationLaunchUserNotificationKey: AnyHashable = NSApplication.launchUserNotificationUserInfoKey
        if let notification = aNotification.userInfo?[NSApplicationLaunchUserNotificationKey] as? NSUserNotification {
            let command = notification.userInfo?["command"] as? String
            runCommandOnShell(command!)
            exit(0)
        }

        // Usage: <notifier-path> <title> <message> <command>
        if CommandLine.argc != 4 {
            print("Usage:", CommandLine.arguments[0], "<title> <message> <command>")
            exit(0)
        }

        let title = CommandLine.arguments[1],
            message = CommandLine.arguments[2],
            command = CommandLine.arguments[3]

        NSUserNotificationCenter.default.delegate = self
        showNotification(title: title, message: message, command: command)
        DispatchQueue.main.asyncAfter(deadline: .now() + 1.0) {
           exit(0)
        }
    }

    func  applicationWillTerminate(_ aNotification: Notification) {
    }

    func runCommandOnShell(_ command: String) {
        let process = Process()
        process.arguments = ["-c", command]
        process.launchPath = "/bin/sh"
        process.launch()
    }
    
    func showNotification(title: String, message: String, command: String) -> Void {
        let notification: NSUserNotification = NSUserNotification()

        notification.title = title
        notification.informativeText = message
        notification.userInfo = [
            "command": command,
        ];

        NSUserNotificationCenter.default.deliver(notification)
    }
}
