import {observer} from "mobx-react-lite";
import {useParams} from "react-router";
import React from "react";
import {ProjectsList} from "../Components/ProjectsList";
import {NoProjectSelected} from "../Components/NoProjectSelected";
import {SingleHistoryPanel} from "../Components/SingleHistoryPanel";

export const SingleHistoryPage = observer(()=>{
    const {conversionJobId} = useParams();
    return <React.Fragment>
        <ProjectsList/>
        {conversionJobId ? <SingleHistoryPanel/> : <NoProjectSelected/>}
    </React.Fragment>
})