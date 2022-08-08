; ModuleID = 'TheTestApplication'
source_filename = "TheTestApplication"
target datalayout = "e-m:w-p270:32:32-p271:32:32-p272:64:64-i64:64-f80:128-n8:16:32:64-S128"
target triple = "x86_64-pc-windows-msvc"

@strtmp = private unnamed_addr constant [14 x i8] c"Hello, world!\00", align 1

define i32 @main() {
entry:
  %x = alloca i32, align 4
  store i32 250, i32* %x, align 4
  br i1 true, label %then, label %ifcont

then:                                             ; preds = %entry
  %CallTMP = call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([14 x i8], [14 x i8]* @strtmp, i32 0, i32 0))
  br label %ifcont

else:                                             ; No predecessors!
  br label %ifcont

ifcont:                                           ; preds = %then, %else, %entry
  ret i32 1
}

declare i32 @printf(i8*, ...)
