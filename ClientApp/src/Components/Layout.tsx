import React from 'react';
import {observer} from "mobx-react-lite";
import {appState} from "../App";
import ErrorBar from "./ErrorBar";
import {Header} from "./Header";

export const Layout = observer(({children}: {children: any})=>{
    return (
        <React.Fragment>
            {appState.errors.map((val, index)=><ErrorBar error={val} key={index}/>)}
            <Header user={appState.user!}/>
            <main>{children}</main>
        </React.Fragment>
    )
})