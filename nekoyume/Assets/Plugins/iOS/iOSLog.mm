//Used to print log on xcode console

extern "C"{
    void Log(const char* msg){
        NSLog(@"iOSLog:%s", msg);
    }
}
