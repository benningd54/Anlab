﻿import * as React from 'react';
import { shallow, mount, render } from 'enzyme';
import { ITestItem } from '../TestList';
import { Summary } from '../Summary';

describe('<Summary />', () => {
    describe('Internal functions', () => {
        const testItems: Array<ITestItem> = [
            { id: 1, analysis: '1ABC', code: '1C-ABC', internalCost: 2.02, externalCost: 3.03, internalSetupCost: 5, externalSetupCost: 7,category: 'Cat1' , notes: ''},
            { id: 2, analysis: '2ABC', code: '2C-ABC', internalCost: 1.01, externalCost: 4.03, internalSetupCost: 6, externalSetupCost: 7, category: 'Cat2', notes: '' }
        ];

        describe('totalCost internal function', () => {
            it('should add up the cost with internal, quantity 1, no grind/filter/foreign', () => {
                const payment = { clientType: 'uc', account: '' };
                const target = shallow(<Summary adjustmentAmount={0}
                                                isFromLab={false}
                                                quantity={1}
                                                payment={payment}
                                                status="Test"
                                                canSubmit={false}
                                                hideError={true}
                                                isCreate={true}
                                                onSubmit={null}
                                                testItems={testItems} />);
                const internal = target.instance();
                expect(internal.totalCost()).toEqual(14.03);
            });
            it('should add up the cost with internal, quantity 3, no grind/filter/foreign', () => {
                const payment = { clientType: 'uc', account: '' };
                const target = shallow(<Summary adjustmentAmount={0}
                                                isFromLab={false}
                                                quantity={3}
                                                payment={payment}
                                                status="Test"
                                                canSubmit={false}
                                                hideError={true}
                                                isCreate={true}
                                                onSubmit={null}
                                                testItems={testItems} />);
                const internal = target.instance();
                expect(internal.totalCost()).toEqual(20.09); //(3 * 3.03) + 11
            });
            xit('should add up the cost with external, quantity 1, no grind/filter/foreign', () => {
                const payment = { clientType: 'other', account: '' };
                const target = shallow(<Summary adjustmentAmount={0}
                                                isFromLab={false}
                                                quantity={1}
                                                payment={payment}
                                                status="Test"
                                                canSubmit={false}
                                                hideError={true}
                                                isCreate={true}
                                                onSubmit={null}
                                                testItems={testItems} />);
                const internal = target.instance();
                expect(internal.totalCost()).toEqual(18.06); //(7.06 + 11 )
            });
            xit('should add up the cost with external, quantity 3, no grind/filter/foreign', () => {
                const payment = { clientType: 'other', account: '' };
                const target = shallow(<Summary adjustmentAmount={0}
                                                isFromLab={false}
                                                quantity={3}
                                                payment={payment}
                                                status="Test"
                                                canSubmit={false}
                                                hideError={true}
                                                isCreate={true}
                                                onSubmit={null}
                                                testItems={testItems} />);
                const internal = target.instance();
                expect(internal.totalCost()).toEqual(32.18); //(3 * 7.06) + 11
            });
        });
        describe('_renderAdditionalFees internal function', () =>
        {
            it('should return null', () => {
                const payment = { clientType: 'uc', account: '' };
                const target = shallow(<Summary adjustmentAmount={0}
                                                isFromLab={false}
                                                quantity={1}
                                                payment={payment}
                                                status="Test"
                                                canSubmit={false}
                                                hideError={true}
                                                isCreate={true}
                                                onSubmit={null}
                                                testItems={testItems} />);
                const internal = target.instance();
                //console.log(internal._renderAdditionalFees().debug());
                expect(internal._renderAdditionalFees()).toEqual(null);
            });

            it('should render when adjustment is not zero 1', () => {
                const payment = { clientType: 'uc', account: '' };
                const target = shallow(<Summary adjustmentAmount={0.01}
                                                isFromLab={false}
                                                quantity={1}
                                                payment={payment}
                                                
                                                status="Test"
                                                canSubmit={false}
                                                
                                                
                                                hideError={true}
                                                isCreate={true}
                                                onSubmit={null}
                                                testItems={testItems} />);
                const internal = target.instance();
                expect(internal._renderAdditionalFees()).not.toEqual(null);
            });
            it('should render when adjustment is not zero 2', () => {
                const payment = { clientType: 'uc', account: '' };
                const target = shallow(<Summary adjustmentAmount={-1}
                                                isFromLab={false}
                                                quantity={1}
                                                payment={payment}
                                                
                                                status="Test"
                                                canSubmit={false}
                                                
                                                
                                                hideError={true}
                                                isCreate={true}
                                                onSubmit={null}
                                                testItems={testItems} />);
                const internal = target.instance();
                expect(internal._renderAdditionalFees()).not.toEqual(null);
            });
            it('should render when all conditions are set', () => {
                const payment = { clientType: 'uc', account: '' };
                const target = shallow(<Summary adjustmentAmount={10}
                                                isFromLab={false}
                                                quantity={1}
                                                payment={payment}
                                                
                                                status="Test"
                                                canSubmit={false}
                                                
                                                
                                                hideError={true}
                                                isCreate={true}
                                                onSubmit={null}
                                                testItems={testItems} />);
                const internal = target.instance();
                expect(internal._renderAdditionalFees()).not.toEqual(null);
            });
        });
    });

    describe('Rendering', () => {
        it('should render nothing if no tests are selected', () => {
            const testItems: Array<ITestItem> = [];
            const payment = { clientType: 'other', account: '' };
            const target = mount(<Summary adjustmentAmount={0} isFromLab={false} quantity={1} payment={payment}  status="Test" canSubmit={false}   hideError={true} isCreate={true} onSubmit={null} testItems={testItems} />);
            expect(target.find('div').length).toEqual(0);
        });
        it('should render something if tests are selected', () => {
            const testItems: Array<ITestItem> = [
                { id: 1, analysis: '1ABC', code: '1C-ABC', internalCost: 2.02, externalCost: 3.03, internalSetupCost: 5, externalSetupCost: 7, category: 'Cat1', notes: '' },
                { id: 2, analysis: '2ABC', code: '2C-ABC', internalCost: 1.01, externalCost: 4.03, internalSetupCost: 6, externalSetupCost: 7, category: 'Cat2', notes: '' }
            ]
            const payment = { clientType: 'other', account: '' };
            const target = mount(<Summary adjustmentAmount={0} isFromLab={false} quantity={1} payment={payment}  status="Test" canSubmit={false}   hideError={true} isCreate={true} onSubmit={null} testItems={testItems} />);
            expect(target.find('div').length).toBeGreaterThan(0);
        });
    });
});
