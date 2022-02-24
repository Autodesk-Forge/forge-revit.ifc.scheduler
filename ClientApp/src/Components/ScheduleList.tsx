import React from 'react';
import {observer} from 'mobx-react-lite';
import {useHistory, useParams} from "react-router-dom";
import {useTranslation} from "react-i18next";
import {SearchableAndSortableTable} from "./SearchableAndSortableTable";
import {appState} from "../App";
import {Schedule} from "../Utilities/DataTypes/Schedule";

export const ScheduleList = observer(()=>{
    const { t } = useTranslation();
    const {projectId, scheduleId} = useParams();
    const history = useHistory();
    const currentProject = appState.projects.find(val=>val.projectId === projectId);

    return (
        <React.Fragment>

            <SearchableAndSortableTable
                title={t("Schedules")}
                placeholder={t("Search Schedules")}
                items={currentProject?.schedules || []}
                onItemInvoked={(item: Schedule)=>{
                    history.push(`/projects/${projectId}/schedules/${item.id}`)}}
                columns={[{key: 'name', fieldName: 'name', minWidth: 20, name: t("Schedule Name")}]}
                selectedRowId={scheduleId}
            />

            <div style={{borderLeft: '1px solid grey'}}/>
        </React.Fragment>
        )
})